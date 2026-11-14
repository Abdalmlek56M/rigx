using System.Globalization;
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;
using HardwareStore.Common.Domain;
using HardwareStore.Modules.Auditing;
using HardwareStore.Modules.Catalog;
using HardwareStore.Modules.Identity;
using HardwareStore.Modules.Identity.Authentication;
using HardwareStore.Modules.Identity.Authorization;
using HardwareStore.Modules.Identity.Persistence;
using HardwareStore.Modules.Inventory;
using HardwareStore.Modules.Storefront;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using SkiaSharp;

public sealed class PreviewOptions
{
    public bool Enabled { get; init; }
    public required string BootstrapKey { get; init; }
    public required string OwnerEmail { get; init; }
    public required string OwnerUsername { get; init; }
    public required string OwnerFullName { get; init; }
    public required string MigrationConnectionString { get; init; }
    public string RuntimeConnectionString { get; init; } = string.Empty;
}

public sealed record PreviewInitializationResult(
    int Categories,
    int Brands,
    int Products,
    string OwnerUsername);

public sealed class PreviewInitializationService(
    PreviewOptions options,
    IHostEnvironment environment,
    TimeProvider timeProvider,
    TimeZoneInfo businessTimeZone,
    ICatalogMediaStorage mediaStorage,
    ILogger<PreviewInitializationService> logger)
{
    private static readonly SemaphoreSlim Gate = new(1, 1);

    public async Task<PreviewInitializationResult> InitializeAsync(
        string bootstrapKey,
        CancellationToken cancellationToken = default)
    {
        EnsureEnabled(bootstrapKey);
        EnsureMigrationTargetMatchesRuntime();
        await Gate.WaitAsync(cancellationToken);
        try
        {
            var identityOptions = new DbContextOptionsBuilder<IdentityDbContext>()
                .UseNpgsql(options.MigrationConnectionString).Options;
            var auditOptions = new DbContextOptionsBuilder<SecurityAuditDbContext>()
                .UseNpgsql(options.MigrationConnectionString).Options;
            var catalogOptions = new DbContextOptionsBuilder<CatalogDbContext>()
                .UseNpgsql(options.MigrationConnectionString).Options;

            await DatabaseMigrator.MigrateAllAsync(options.MigrationConnectionString, logger, cancellationToken);
            await using var identity = new IdentityDbContext(identityOptions);
            await using var audit = new SecurityAuditDbContext(auditOptions);
            await using var catalog = new CatalogDbContext(catalogOptions);

            var owner = await EnsureOwnerAsync(identity, audit, cancellationToken);
            var seeded = await SeedCatalogAsync(catalog, cancellationToken);
            logger.LogInformation(
                "Development preview initialized: {Categories} categories, {Brands} brands, {Products} products.",
                seeded.Categories, seeded.Brands, seeded.Products);
            return new PreviewInitializationResult(
                seeded.Categories, seeded.Brands, seeded.Products, owner.Username);
        }
        finally
        {
            Gate.Release();
        }
    }

    private void EnsureEnabled(string bootstrapKey)
    {
        if (!environment.IsDevelopment() || !options.Enabled)
            throw new InvalidOperationException("Preview initialization is available only in Development when explicitly enabled.");
        if (string.IsNullOrWhiteSpace(options.OwnerEmail) ||
            string.IsNullOrWhiteSpace(options.OwnerUsername) ||
            string.IsNullOrWhiteSpace(options.OwnerFullName) ||
            string.IsNullOrWhiteSpace(options.MigrationConnectionString))
            throw new InvalidOperationException("Preview owner and migration settings are required.");
        if (!new EmailAddressAttribute().IsValid(options.OwnerEmail) ||
            options.OwnerUsername.Trim().Length > 128 ||
            options.OwnerFullName.Trim().Length > 255)
            throw new InvalidOperationException("Preview owner identity settings are invalid.");
        if (string.IsNullOrWhiteSpace(options.BootstrapKey) ||
            !CryptographicOperations.FixedTimeEquals(
                SHA256.HashData(Encoding.UTF8.GetBytes(bootstrapKey ?? string.Empty)),
                SHA256.HashData(Encoding.UTF8.GetBytes(options.BootstrapKey))))
            throw new UnauthorizedAccessException("Invalid preview bootstrap key.");
    }

    private void EnsureMigrationTargetMatchesRuntime()
    {
        if (string.IsNullOrWhiteSpace(options.RuntimeConnectionString))
            throw new InvalidOperationException("Preview runtime connection string is required.");
        if (string.IsNullOrWhiteSpace(options.MigrationConnectionString))
            throw new InvalidOperationException("Preview migration connection string is required.");

        var runtime = new NpgsqlConnectionStringBuilder(options.RuntimeConnectionString);
        var migrations = new NpgsqlConnectionStringBuilder(options.MigrationConnectionString);
        var sameHost = string.Equals(runtime.Host, migrations.Host, StringComparison.OrdinalIgnoreCase);
        var samePort = runtime.Port == migrations.Port;
        var sameDatabase = string.Equals(runtime.Database, migrations.Database, StringComparison.OrdinalIgnoreCase);
        if (!sameHost || !samePort || !sameDatabase)
            throw new InvalidOperationException(
                "Preview migrations must target the same host, port, and database as the runtime connection. Username and password may differ.");
    }

    private async Task<UserAccount> EnsureOwnerAsync(
        IdentityDbContext db,
        SecurityAuditDbContext audit,
        CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var now = timeProvider.GetUtcNow().ToUniversalTime();
        var ownerRole = await db.Roles.SingleOrDefaultAsync(role => role.Code == "Owner", cancellationToken);
        if (ownerRole is null)
        {
            ownerRole = new Role
            {
                Id = StableGuid("role:owner"),
                Code = "Owner",
                Name = "Owner"
            };
            db.Roles.Add(ownerRole);
        }

        var existingGrants = await db.RolePermissions
            .Where(grant => grant.RoleId == ownerRole.Id)
            .Select(grant => grant.PermissionCode)
            .ToListAsync(cancellationToken);
        var missingPermissionCodes = PermissionCatalog.All
            .Except(existingGrants, StringComparer.Ordinal)
            .ToArray();
        foreach (var permission in missingPermissionCodes)
            db.RolePermissions.Add(new RolePermission
            {
                RoleId = ownerRole.Id,
                PermissionCode = permission
            });

        var normalizedEmail = IdentityLoginService.NormalizeIdentifier(options.OwnerEmail);
        var configuredUsername = options.OwnerUsername.Trim();
        var normalizedUsername = IdentityLoginService.NormalizeIdentifier(configuredUsername);
        var ownerAccounts = await db.UserAccounts
            .Include(item => item.Person)
            .Include(item => item.UserRoles)
            .Where(item => item.UserRoles.Any(userRole => userRole.RoleId == ownerRole.Id))
            .Take(2)
            .ToListAsync(cancellationToken);
        if (ownerAccounts.Count > 1)
            throw new InvalidOperationException("Preview contains more than one Owner account.");

        var account = ownerAccounts.SingleOrDefault();
        if (account is null)
        {
            var matchingAccount = await db.UserAccounts
                .Include(item => item.Person)
                .Include(item => item.UserRoles)
                .SingleOrDefaultAsync(item =>
                    item.NormalizedEmail == normalizedEmail ||
                    item.NormalizedUsername == normalizedUsername,
                    cancellationToken);
            if (matchingAccount is not null)
                throw new InvalidOperationException("The configured preview owner identity belongs to a non-owner account.");
        }
        else
        {
            var identityCollision = await db.UserAccounts.AnyAsync(item =>
                item.Id != account.Id &&
                (item.NormalizedEmail == normalizedEmail || item.NormalizedUsername == normalizedUsername),
                cancellationToken);
            if (identityCollision)
                throw new InvalidOperationException("The configured preview owner username or email is already in use.");
        }

        if (account is not null)
        {
            ApplyConfiguredOwnerProfile(account, configuredUsername, options.OwnerEmail, options.OwnerFullName);
            account.AuthorizationVersion++;
            await db.AuthSessions
                .Where(session => session.UserAccountId == account.Id && session.RevokedAtUtc == null)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(session => session.RevokedAtUtc, now)
                    .SetProperty(session => session.RevokeReason, "PreviewOwnerBootstrap")
                    .SetProperty(session => session.RowVersion, session => session.RowVersion + 1),
                    cancellationToken);
            if (missingPermissionCodes.Length > 0)
            {
                var state = await db.AuthorizationStates.SingleAsync(cancellationToken);
                state.RbacRevision++;
                state.UpdatedAtUtc = now;
            }
            await db.SaveChangesAsync(cancellationToken);
        }
        else
        {
            var person = new Person
            {
                Id = Guid.CreateVersion7(),
                FullName = options.OwnerFullName,
                Email = options.OwnerEmail,
                IsEmployee = true,
                IsActive = true
            };
            await new PersonCodeGenerator(db, timeProvider, businessTimeZone)
                .CreatePersonInCurrentTransactionAsync(person, cancellationToken);
            account = new UserAccount
            {
                Id = Guid.CreateVersion7(),
                PersonId = person.Id,
                Person = person,
                Username = configuredUsername,
                NormalizedUsername = normalizedUsername,
                Email = options.OwnerEmail,
                NormalizedEmail = normalizedEmail,
                IsEmailVerified = true,
                IsActive = true,
                CreatedAtUtc = now
            };
            db.UserAccounts.Add(account);
            db.UserRoles.Add(new UserRole { UserAccountId = account.Id, RoleId = ownerRole.Id });
            var state = await db.AuthorizationStates.SingleAsync(cancellationToken);
            state.RbacRevision++;
            state.UpdatedAtUtc = now;
            await db.SaveChangesAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
        if (account.Id != Guid.Empty)
        {
            audit.SecurityAuditEvents.Add(new SecurityAuditEvent
            {
                Id = Guid.NewGuid(),
                EventType = "Preview.OwnerInitialized",
                OccurredAtUtc = timeProvider.GetUtcNow(),
                Outcome = "Succeeded",
                ActorKind = "System",
                SubjectUserAccountId = account.Id,
                SafeMetadata = "{\"environment\":\"Development\"}"
            });
            await audit.SaveChangesAsync(cancellationToken);
        }
        return account;
    }

    internal static void ApplyConfiguredOwnerProfile(
        UserAccount account,
        string username,
        string email,
        string fullName)
    {
        var trimmedUsername = username.Trim();
        account.Username = trimmedUsername;
        account.NormalizedUsername = IdentityLoginService.NormalizeIdentifier(trimmedUsername);
        account.Email = email;
        account.NormalizedEmail = IdentityLoginService.NormalizeIdentifier(email);
        account.Person.FullName = fullName.Trim();
        account.Person.Email = email;
        account.Person.IsActive = true;
        account.Person.IsEmployee = true;
        account.IsEmailVerified = true;
        account.IsActive = true;
    }

    private async Task<(int Categories, int Brands, int Products)> SeedCatalogAsync(
        CatalogDbContext db,
        CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow().ToUniversalTime();
        var categorySpecs = new[]
        {
            ("Processors", "processors", (string?)null),
            ("AMD Processors", "amd-processors", "processors"),
            ("Intel Processors", "intel-processors", "processors"),
            ("Graphics Cards", "graphics-cards", (string?)null),
            ("Motherboards", "motherboards", (string?)null),
            ("Memory", "memory", (string?)null),
            ("Storage", "storage", (string?)null),
            ("Cooling", "cooling", (string?)null),
            ("Power Supplies", "power-supplies", (string?)null),
            ("Cases", "cases", (string?)null),
            ("Peripherals", "peripherals", (string?)null)
        };
        var categories = await db.Categories.ToDictionaryAsync(item => item.Slug, cancellationToken);
        foreach (var (name, slug, parentSlug) in categorySpecs)
        {
            if (categories.ContainsKey(slug))
                continue;
            var parent = parentSlug is null ? null : categories[parentSlug];
            var category = new CatalogCategory
            {
                Id = StableGuid($"category:{slug}"),
                ParentId = parent?.Id,
                Name = name,
                Slug = slug,
                SortOrder = categorySpecs.Length - categories.Count,
                IsActive = true,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            };
            db.Categories.Add(category);
            categories[slug] = category;
        }
        await db.SaveChangesAsync(cancellationToken);
        foreach (var category in categorySpecs
                     .Select(spec => categories[spec.Item2])
                     .Where(category => category.IconStorageKey is null))
        {
            var assetId = StableGuid($"category-image:{category.Slug}");
            var expectedKey = $"categories/{category.Id:N}/{assetId:N}.webp";
            await mediaStorage.DeleteStorageKeyAsync(expectedKey, cancellationToken);
            category.IconStorageKey = await mediaStorage.SaveAssetImageAsync(
                "categories",
                category.Id,
                assetId,
                CreatePlaceholderUpload(category.Slug.GetHashCode(StringComparison.Ordinal)),
                cancellationToken);
        }
        await db.SaveChangesAsync(cancellationToken);

        var brandNames = new[]
        {
            "AMD", "Intel", "NVIDIA", "ASUS", "Corsair", "Samsung", "Kingston", "NZXT", "Lian Li", "Logitech"
        };
        var brands = await db.Brands.ToDictionaryAsync(item => item.Name, cancellationToken);
        foreach (var brandName in brandNames)
        {
            if (brands.ContainsKey(brandName))
                continue;
            var brand = new CatalogBrand
            {
                Id = StableGuid($"brand:{brandName}"),
                Name = brandName,
                Slug = brandName.ToLowerInvariant().Replace(' ', '-'),
                IsActive = true,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            };
            db.Brands.Add(brand);
            brands[brandName] = brand;
        }
        await db.SaveChangesAsync(cancellationToken);
        foreach (var brand in brandNames.Select(name => brands[name]).Where(brand => brand.LogoStorageKey is null))
        {
            var assetId = StableGuid($"brand-image:{brand.Name}");
            var expectedKey = $"brands/{brand.Id:N}/{assetId:N}.webp";
            await mediaStorage.DeleteStorageKeyAsync(expectedKey, cancellationToken);
            brand.LogoStorageKey = await mediaStorage.SaveAssetImageAsync(
                "brands",
                brand.Id,
                assetId,
                CreatePlaceholderUpload(brand.Name.GetHashCode(StringComparison.Ordinal)),
                cancellationToken);
        }
        await db.SaveChangesAsync(cancellationToken);

        var seeds = PreviewProductDefinitions();
        foreach (var seed in seeds)
        {
            var productId = StableGuid($"product:{seed.Slug}");
            if (!await db.Products.AnyAsync(item => item.Id == productId, cancellationToken))
            {
                var product = new CatalogProduct
                {
                    Id = productId,
                    CategoryId = categories[seed.CategorySlug].Id,
                    BrandId = brands[seed.BrandName].Id,
                    Name = seed.Name,
                    Slug = seed.Slug,
                    Sku = seed.Sku,
                    ShortDescription = seed.ShortDescription,
                    Description = seed.Description,
                    BasePrice = seed.BasePrice,
                    Status = seed.Status,
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now
                };
                foreach (var specification in seed.Specifications)
                {
                    product.Specifications.Add(new CatalogProductSpecification
                    {
                        Id = StableGuid($"spec:{seed.Slug}:{specification.Group}:{specification.Name}"),
                        GroupName = specification.Group,
                        Name = specification.Name,
                        Value = specification.Value,
                        SortOrder = product.Specifications.Count
                    });
                }
                db.Products.Add(product);
                await db.SaveChangesAsync(cancellationToken);
            }

            if (!await db.ProductImages.AnyAsync(
                    image => image.ProductId == productId,
                    cancellationToken))
            {
                var imageId = StableGuid($"image:{seed.Slug}");
                var source = CreatePlaceholderUpload(seed.Slug.GetHashCode(StringComparison.Ordinal));
                await mediaStorage.DeleteAsync(
                    $"products/{productId:N}/{imageId:N}.webp", cancellationToken);
                var stored = await mediaStorage.SaveImageAsync(
                    productId, imageId, source, cancellationToken: cancellationToken);
                db.ProductImages.Add(new CatalogProductImage
                {
                    Id = imageId,
                    ProductId = productId,
                    StorageKey = stored.StorageKey,
                    ThumbnailStorageKey = stored.ThumbnailStorageKey,
                    AltText = seed.Name,
                    FileSizeBytes = stored.FileSizeBytes,
                    Width = stored.Width,
                    Height = stored.Height,
                    ThumbnailWidth = stored.ThumbnailWidth,
                    ThumbnailHeight = stored.ThumbnailHeight,
                    SortOrder = 0,
                    IsPrimary = true,
                    CreatedAtUtc = now
                });
                await db.SaveChangesAsync(cancellationToken);
            }
        }
        return (categories.Count, brands.Count, seeds.Length);
    }

    private static CatalogImageUpload CreatePlaceholderUpload(int seed)
    {
        const int width = 1400;
        const int height = 900;
        using var bitmap = new SKBitmap(new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul));
        using var canvas = new SKCanvas(bitmap);
        var hue = Math.Abs(seed % 360);
        using var background = new SKPaint
        {
            Shader = SKShader.CreateLinearGradient(
                new SKPoint(0, 0),
                new SKPoint(width, height),
                [SKColor.FromHsl(hue, 70, 22), SKColor.FromHsl((hue + 45) % 360, 75, 48)],
                null,
                SKShaderTileMode.Clamp)
        };
        canvas.DrawRect(0, 0, width, height, background);
        using var shape = new SKPaint { Color = new SKColor(255, 255, 255, 34), IsAntialias = true };
        canvas.DrawCircle(230, 180, 170, shape);
        canvas.DrawCircle(1160, 720, 260, shape);
        using var line = new SKPaint
        {
            Color = new SKColor(255, 255, 255, 190),
            StrokeWidth = 18,
            IsAntialias = true,
            Style = SKPaintStyle.Stroke
        };
        canvas.DrawRoundRect(new SKRect(330, 285, 1070, 615), 48, 48, line);
        canvas.Flush();
        using var data = SKImage.FromBitmap(bitmap).Encode(SKEncodedImageFormat.Png, 100)
            ?? throw new InvalidOperationException("Preview image generation failed.");
        var bytes = data.ToArray();
        return new CatalogImageUpload(
            new MemoryStream(bytes, writable: false),
            bytes.LongLength,
            "preview.png",
            "image/png");
    }

    private static PreviewProductSeed[] PreviewProductDefinitions() =>
    [
        P("AMD Ryzen 7 9800X3D", "amd-ryzen-7-9800x3d", "CPU-9800X3D", "amd-processors", "AMD", 25990m, "AMD processors",
            [S("Platform", "Socket", "AM5")]),
        P("Intel Core Ultra 7 265K", "intel-core-ultra-7-265k", "CPU-265K", "intel-processors", "Intel", 21490m, "Intel processors",
            [S("Platform", "Socket", "LGA1851")]),
        P("NVIDIA GeForce RTX 5080 16GB", "nvidia-geforce-rtx-5080-16gb", "GPU-5080", "graphics-cards", "NVIDIA", 57990m, "graphics cards",
            [S("Graphics", "Memory", "16 GB GDDR7"), S("Graphics", "Cooling", "Triple Fan")]),
        P("AMD Radeon RX 9070 XT 16GB", "amd-radeon-rx-9070-xt-16gb", "GPU-9070XT", "graphics-cards", "AMD", 38990m, "graphics cards",
            [S("Graphics", "Memory", "16 GB GDDR6"), S("Graphics", "Interface", "PCIe 5.0")]),
        P("ASUS ROG Strix B650E-F", "asus-rog-strix-b650e-f", "MB-B650E-F", "motherboards", "ASUS", 12490m, "motherboards",
            [S("Platform", "Socket", "AM5"), S("Platform", "Form Factor", "ATX")]),
        P("Corsair Vengeance 32GB DDR5-6000", "corsair-vengeance-32gb-ddr5-6000", "RAM-VEN32", "memory", "Corsair", 5490m, "memory",
            [S("Memory", "Capacity", "32 GB"), S("Memory", "Speed", "6000 MT/s")]),
        P("Samsung 990 PRO 2TB NVMe", "samsung-990-pro-2tb", "SSD-990P2", "storage", "Samsung", 8390m, "storage",
            [S("Storage", "Interface", "PCIe 4.0 x4")]),
        P("Kingston NV3 1TB NVMe", "kingston-nv3-1tb", "SSD-NV3-1", "storage", "Kingston", 2790m, "storage",
            [S("Storage", "Interface", "PCIe 4.0 x4")]),
        P("NZXT Kraken 360 RGB", "nzxt-kraken-360-rgb", "COOL-K360", "cooling", "NZXT", 9890m, "cooling",
            [S("Cooling", "Radiator", "360 mm")]),
        P("Corsair RM850e Gold", "corsair-rm850e-gold", "PSU-RM850E", "power-supplies", "Corsair", 6490m, "power supplies",
            [S("Power", "Wattage", "850 W"), S("Power", "Efficiency", "80+ Gold")]),
        P("Lian Li O11 Vision", "lian-li-o11-vision", "CASE-O11V", "cases", "Lian Li", 7290m, "cases",
            [S("Case", "Form Factor", "Mid Tower")]),
        P("Logitech G Pro X Superlight 2", "logitech-g-pro-x-superlight-2", "MOU-GPX2", "peripherals", "Logitech", 5990m, "peripherals",
            [S("Mouse", "Weight", "60 g")]),
        P("ASUS TUF Gaming 27 QHD", "asus-tuf-gaming-27-qhd", "MON-TUF27", "peripherals", "ASUS", 13990m, "peripherals",
            [S("Display", "Resolution", "2560 x 1440")], CatalogProductStatus.Draft),
        P("Corsair iCUE Link Titan 360", "corsair-icue-link-titan-360", "COOL-TITAN360", "cooling", "Corsair", 11990m, "cooling",
            [S("Cooling", "Radiator", "360 mm")], CatalogProductStatus.Draft)
    ];

    private static PreviewProductSeed P(
        string name,
        string slug,
        string sku,
        string categorySlug,
        string brandName,
        decimal price,
        string descriptionKey,
        PreviewSpecSeed[] specs,
        CatalogProductStatus status = CatalogProductStatus.Published) =>
        new(name, slug, sku, categorySlug, brandName, price,
            $"Demo catalog item for {descriptionKey}.",
            $"Local preview product. {name} is seeded only for Development UI testing.",
            specs, status);

    private static PreviewSpecSeed S(string group, string name, string value) =>
        new(group, name, value);

    private static Guid StableGuid(string value)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        var bytes = hash[..16];
        bytes[7] = (byte)((bytes[7] & 0x0F) | 0x70);
        bytes[8] = (byte)((bytes[8] & 0x3F) | 0x80);
        return new Guid(bytes);
    }

    private sealed record PreviewProductSeed(
        string Name,
        string Slug,
        string Sku,
        string CategorySlug,
        string BrandName,
        decimal BasePrice,
        string ShortDescription,
        string Description,
        PreviewSpecSeed[] Specifications,
        CatalogProductStatus Status);

    private sealed record PreviewSpecSeed(string Group, string Name, string Value);
}

public static class PreviewInitializationEndpoints
{
    public static IEndpointRouteBuilder MapPreviewInitializationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/v1/development/preview/initialize", InitializeAsync)
            .AllowAnonymous();
        return endpoints;
    }

    private static async Task<Ok<PreviewInitializationResult>> InitializeAsync(
        HttpContext context,
        PreviewInitializationService service,
        CancellationToken cancellationToken)
    {
        var key = context.Request.Headers["X-Preview-Bootstrap-Key"].ToString();
        try
        {
            return TypedResults.Ok(await service.InitializeAsync(key, cancellationToken));
        }
        catch (UnauthorizedAccessException)
        {
            throw new UnauthenticatedException();
        }
        catch (InvalidOperationException exception)
        {
            throw new ApiProblemException(StatusCodes.Status409Conflict, exception.Message);
        }
    }
}

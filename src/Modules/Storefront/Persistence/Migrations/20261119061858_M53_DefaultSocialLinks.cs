using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HardwareStore.Modules.Storefront.Persistence.Migrations
{
    /// <summary>
    /// Adds the store's social accounts (handle <c>rigxhardware</c>, owner decision 19 Nov 2026) to a saved contact list, once,
    /// for each platform that has no link yet. A store that never saved its list already gets them from the defaults.
    /// </summary>
    public partial class M53_DefaultSocialLinks : Migration
    {
        private static readonly (string Platform, string Url)[] Links =
        [
            ("Facebook", "https://www.facebook.com/rigxhardware"),
            ("Instagram", "https://www.instagram.com/rigxhardware"),
            ("TikTok", "https://www.tiktok.com/@rigxhardware"),
            ("YouTube", "https://www.youtube.com/@rigxhardware"),
            ("Telegram", "https://t.me/rigxhardware")
        ];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var (platform, url) in Links)
                migrationBuilder.Sql($$"""
                    UPDATE store_settings
                    SET contact_links = jsonb_set(contact_links, '{links}', (contact_links -> 'links') || jsonb_build_array(jsonb_build_object(
                            'platform', '{{platform}}', 'value', '{{url}}', 'showOnSite', true, 'showOnInvoice', false, 'showInFloatingButton', false))),
                        row_version = row_version + 1,
                        updated_at_utc = now()
                    WHERE jsonb_typeof(contact_links -> 'links') = 'array'
                      AND jsonb_array_length(contact_links -> 'links') < 20
                      AND NOT EXISTS (SELECT 1 FROM jsonb_array_elements(contact_links -> 'links') AS link WHERE link ->> 'platform' = '{{platform}}');
                    """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Data only: the links stay, the owner removes any of them in the settings.
        }
    }
}

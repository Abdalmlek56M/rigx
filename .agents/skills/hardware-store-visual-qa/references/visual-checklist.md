# Visual QA Checklist

- Page title and URL match the intended route.
- Meaningful content renders; no blank shell or framework overlay.
- Console has no relevant application errors.
- First viewport has a clear product/catalog hierarchy.
- Desktop and mobile layouts have no horizontal overflow or clipped controls.
- RTL and LTR place sidebars, icons, and field adornments correctly.
- Light and Dark themes preserve contrast and distinguishable states.
- Keyboard focus is visible; dialogs/drawers can be closed without a pointer.
- Loading, empty, and error states are reachable and truthful.
- Images have stable dimensions and do not cause layout shifts.
- Pointer tilt is absent on touch devices and reduced-motion mode.
- Screenshot evidence is inspected directly with `view_image` when available.

## Global Background and Footer

- [ ] Capture a long page and a short page before/after.
- [ ] Background treatment is continuous from the top of the viewport to the bottom edge of the footer.
- [ ] Footer surface is transparent/subtle and does not hide the ambient layer or create an unrelated opaque block.
- [ ] No duplicated fixed background layers, canvases, pointer listeners, RAF loops, or observers.
- [ ] Footer text, separators, focus states, and links retain sufficient contrast in Light and Dark.
- [ ] Verify 390px and 1440px in Arabic/RTL and English/LTR; add affected intermediate breakpoints.
- [ ] Verify Reduced Motion and production/customer mode hide local Preview/M4 markers.

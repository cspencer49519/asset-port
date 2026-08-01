## v1.2.8
- Update Thunderstore package icon to the Real TCG Overhaul Patch logo

## v1.2.7
- Add Thunderstore `CHANGELOG.md` (supported changelog format)
- Package build copies `CHANGELOG.md` into the Thunderstore zip root

## v1.2.6
- Fix graded Destiny/Trainer/Ghost cards on shop display stands showing the mirrored front instead of an opaque card back
- LateUpdate graded-slab face repair re-arms the shelf back instead of wiping it every frame
- Graded shelf `m_CardBack` faces rearward so UI Cull Off GradedFace art is not visible from behind the stand
- More reliable shelf interactable lookup via parent `Card3dUIGroup` registry

## v1.2.4
- Thunderstore README: fingerprinted tested dependency versions from a working local install
- Genobear-first install docs with Nexus links for required mods
- Patch DLL + pre-ported `sharedassets0` trio in this package

## v1.2.3
- Expand Thunderstore install documentation (Genobear prerequisites and sharedassets steps)

## v1.2.2
- Point Thunderstore `website_url` at the GitHub repository

## v1.2.1
- Initial Thunderstore package for the 0.70.3 Genobear compatibility patch

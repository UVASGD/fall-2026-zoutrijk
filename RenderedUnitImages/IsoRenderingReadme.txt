To auto-render units:
- Open the IsoRenderSetup.blend and save a copy with the unit name.
- Replace default cube with the associated unit model + animations
- Install the Pillow py library via "pip install pillow" in cmd
- Go to scripting tab and change the target directory
- Once rendering completes (may take a hot minute on weaker hardware),
copy the RenderedImageSpritesheetStitcher.py and run via cmd to auto-generate a spritesheet. Tweak the script for different parameters.
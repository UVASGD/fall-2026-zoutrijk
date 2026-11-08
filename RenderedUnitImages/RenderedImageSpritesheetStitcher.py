# Put this script inside of your folder with the raw sprites

import os
import math
from PIL import Image

# --- CONFIGURATION ---
input_folder = "./"  # Folder with your rendered frames
output_file = "spritesheet.png"
sprite_width = 256
sprite_height = 256
columns = 8 # Number of frames per animation row

# Gather all PNGs and sort them alphabetically
# The Blender script naming convention naturally groups them by Direction, then Frame
images = [img for img in os.listdir(input_folder) if img.endswith(".png")]
images.sort()

if not images:
    print("No images found.")
    exit()

# Calculate sheet dimensions
total_sprites = len(images)
rows = math.ceil(total_sprites / columns)

sheet_width = columns * sprite_width
sheet_height = rows * sprite_height

# Create a blank transparent image
spritesheet = Image.new("RGBA", (sheet_width, sheet_height), (0, 0, 0, 0))

# Paste each frame into the correct grid position
for index, filename in enumerate(images):
    img_path = os.path.join(input_folder, filename)
    with Image.open(img_path) as sprite:
        # Calculate grid position
        col = index % columns
        row = index // columns
        
        x = col * sprite_width
        y = row * sprite_height
        
        spritesheet.paste(sprite, (x, y))

spritesheet.save(output_file)
print(f"Spritesheet saved to {output_file} at {sheet_width}x{sheet_height}!")

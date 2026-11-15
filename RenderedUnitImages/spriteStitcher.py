import os
import math
from PIL import Image

# --- CONFIGURATION ---
input_folder = "./"  # Folder with your rendered frames
output_file = "spritesheet.png"
columns = 8 # Number of frames per animation row (set to your frame count if you want 1 direction per row)

# Gather all PNGs (excluding the output spritesheet itself)
images = [
    img for img in os.listdir(input_folder) 
    if img.endswith(".png") and img != output_file
]

if not images:
    print("No images found.")
    exit()

# Sort chronologically by render timestamp (with filename as tie-breaker)
images.sort(key=lambda img: (os.path.getmtime(os.path.join(input_folder, img)), img))

# Detect sprite dimensions dynamically from the first PNG
first_img_path = os.path.join(input_folder, images[0])
with Image.open(first_img_path) as first_sprite:
    sprite_width, sprite_height = first_sprite.size

# Calculate sheet dimensions
total_sprites = len(images)
rows = math.ceil(total_sprites / columns)

sheet_width = columns * sprite_width
sheet_height = rows * sprite_height

# Create a blank transparent image
spritesheet = Image.new("RGBA", (sheet_width, sheet_height), (0, 0, 0, 0))
processed_paths = []

# Paste each frame into the correct grid position
for index, filename in enumerate(images):
    img_path = os.path.join(input_folder, filename)
    with Image.open(img_path) as sprite:
        col = index % columns
        row = index // columns
        
        x = col * sprite_width
        y = row * sprite_height
        
        spritesheet.paste(sprite, (x, y))
    
    processed_paths.append(img_path)

# Save the stitched sheet first before deleting any source files
output_path = os.path.join(input_folder, output_file)
spritesheet.save(output_path)
print(f"Spritesheet saved to {output_path} ({sprite_width}x{sprite_height} per sprite, {sheet_width}x{sheet_height} total)!")

# Clean up the old source PNGs
for img_path in processed_paths:
    try:
        os.remove(img_path)
    except OSError as e:
        print(f"Error deleting {img_path}: {e}")

print(f"Deleted {len(processed_paths)} source PNGs.")

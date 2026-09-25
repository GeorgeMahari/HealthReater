export const AVATAR_TYPES = ["image/jpeg", "image/png", "image/webp"];
const AVATAR_EXTENSIONS = /\.(jpe?g|png|webp)$/i;
export const MAX_SOURCE_BYTES = 10 * 1024 * 1024;
const MIN_SIDE = 64;
const OUTPUT_SIDE = 320;

/**
 * Checks the chosen file, then centre-crops it to a square and re-encodes it at 320×320.
 * Re-encoding keeps uploads small and strips embedded metadata (e.g. EXIF/GPS) before the
 * image ever leaves the browser. The server validates the result again independently.
 */
export async function prepareAvatar(file: File): Promise<Blob> {
  if (!AVATAR_TYPES.includes(file.type) || !AVATAR_EXTENSIONS.test(file.name)) {
    throw new Error("Please choose a JPG, PNG or WEBP image.");
  }
  if (file.size > MAX_SOURCE_BYTES) {
    throw new Error("That image is larger than 10 MB. Please choose a smaller one.");
  }

  let bitmap: ImageBitmap;
  try {
    bitmap = await createImageBitmap(file);
  } catch {
    throw new Error("That file couldn't be read as an image.");
  }

  try {
    if (bitmap.width < MIN_SIDE || bitmap.height < MIN_SIDE) {
      throw new Error(`Please choose an image at least ${MIN_SIDE}×${MIN_SIDE} pixels.`);
    }

    const side = Math.min(bitmap.width, bitmap.height);
    const canvas = document.createElement("canvas");
    canvas.width = OUTPUT_SIDE;
    canvas.height = OUTPUT_SIDE;
    const ctx = canvas.getContext("2d");
    if (!ctx) throw new Error("Your browser couldn't process the image.");
    ctx.imageSmoothingQuality = "high";
    ctx.drawImage(
      bitmap,
      (bitmap.width - side) / 2,
      (bitmap.height - side) / 2,
      side,
      side,
      0,
      0,
      OUTPUT_SIDE,
      OUTPUT_SIDE
    );

    const webp = await toBlob(canvas, "image/webp", 0.9);
    // Browsers without WEBP encoding silently return PNG; fall back to JPEG in that case.
    if (webp && webp.type === "image/webp") return webp;
    const jpeg = await toBlob(canvas, "image/jpeg", 0.9);
    if (!jpeg) throw new Error("Your browser couldn't process the image.");
    return jpeg;
  } finally {
    bitmap.close();
  }
}

function toBlob(canvas: HTMLCanvasElement, type: string, quality: number): Promise<Blob | null> {
  return new Promise((resolve) => canvas.toBlob(resolve, type, quality));
}

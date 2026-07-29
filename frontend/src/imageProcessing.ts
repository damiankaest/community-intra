export interface ImageDimensions {
  width: number
  height: number
}

export function fitImage(
  width: number,
  height: number,
  maximumEdge: number,
): ImageDimensions {
  const scale = Math.min(1, maximumEdge / Math.max(width, height))
  return {
    width: Math.max(1, Math.round(width * scale)),
    height: Math.max(1, Math.round(height * scale)),
  }
}

export async function prepareScreenshot(file: File) {
  if (file.type === 'image/gif') {
    return { file, thumbnail: undefined }
  }

  const bitmap = await createImageBitmap(file)
  try {
    const full = fitImage(bitmap.width, bitmap.height, 1920)
    const preview = fitImage(bitmap.width, bitmap.height, 480)
    const fullBlob = await render(bitmap, full, 0.82)
    const thumbnailBlob = await render(bitmap, preview, 0.72)
    const baseName = file.name.replace(/\.[^.]+$/, '') || 'screenshot'
    return {
      file: new File([fullBlob], `${baseName}.webp`, {
        type: 'image/webp',
      }),
      thumbnail: new File([thumbnailBlob], `${baseName}-preview.webp`, {
        type: 'image/webp',
      }),
    }
  } finally {
    bitmap.close()
  }
}

async function render(
  bitmap: ImageBitmap,
  dimensions: ImageDimensions,
  quality: number,
) {
  const canvas = document.createElement('canvas')
  canvas.width = dimensions.width
  canvas.height = dimensions.height
  const context = canvas.getContext('2d')
  if (!context) {
    throw new Error(
      'Bildverarbeitung wird von diesem Browser nicht unterstützt.',
    )
  }

  context.drawImage(bitmap, 0, 0, dimensions.width, dimensions.height)
  return new Promise<Blob>((resolve, reject) =>
    canvas.toBlob(
      (blob) =>
        blob
          ? resolve(blob)
          : reject(new Error('Screenshot konnte nicht verkleinert werden.')),
      'image/webp',
      quality,
    ),
  )
}

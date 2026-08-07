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

  try {
    const bitmap = await createImageBitmap(file)
    try {
      return await prepareImageSource(
        bitmap,
        bitmap.width,
        bitmap.height,
        file.name,
      )
    } finally {
      bitmap.close()
    }
  } catch {
    // Safari/iOS can display camera-library formats that createImageBitmap
    // does not decode (notably HEIC on some versions). Let the native image
    // element decode it and still upload a normalized WebP.
    const image = await loadImage(file)
    return prepareImageSource(
      image,
      image.naturalWidth,
      image.naturalHeight,
      file.name,
    )
  }
}

async function prepareImageSource(
  source: CanvasImageSource,
  width: number,
  height: number,
  fileName: string,
) {
  const full = fitImage(width, height, 1920)
  const preview = fitImage(width, height, 480)
  const fullBlob = await render(source, full, 0.82)
  const thumbnailBlob = await render(source, preview, 0.72)
  const baseName = fileName.replace(/\.[^.]+$/, '') || 'screenshot'
  return {
    file: new File([fullBlob], `${baseName}.webp`, {
      type: 'image/webp',
    }),
    thumbnail: new File([thumbnailBlob], `${baseName}-preview.webp`, {
      type: 'image/webp',
    }),
  }
}

async function loadImage(file: File) {
  const url = URL.createObjectURL(file)
  const image = new Image()
  try {
    await new Promise<void>((resolve, reject) => {
      image.onload = () => resolve()
      image.onerror = () => reject(new Error('Foto konnte nicht gelesen werden.'))
      image.src = url
    })
    return image
  } finally {
    URL.revokeObjectURL(url)
  }
}

async function render(
  source: CanvasImageSource,
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

  context.drawImage(source, 0, 0, dimensions.width, dimensions.height)
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

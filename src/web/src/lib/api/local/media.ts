import { db, queueChange, type Media } from '$lib/db';
import type { MediaDto, MediaOwnerType, MediaType } from '../types';

export interface MediaListResponse {
  items: MediaDto[];
}

export interface AddMediaRequest {
  ownerType: MediaOwnerType;
  ownerId: string;
  storageKey?: string;
  mediaType: MediaType;
  originalFileName?: string;
  mimeType: string;
  sizeBytes: number;
  caption?: string;
  data?: Blob;
}

export interface UpdateMediaRequest {
  caption?: string;
}

function mediaToDto(media: Media): MediaDto {
  const blobUrl = typeof URL !== 'undefined' && media.data
    ? URL.createObjectURL(media.data)
    : undefined;

  return {
    id: media.id,
    zaznamId: media.ownerId,
    ownerType: media.ownerType,
    ownerId: media.ownerId,
    type: media.mediaType,
    storageKey: media.storageKey ?? '',
    originalFileName: media.originalFileName,
    mimeType: media.mimeType,
    sizeBytes: media.sizeBytes,
    caption: media.caption,
    thumbnailUrl: media.thumbnailUrl ?? blobUrl,
    url: media.url ?? blobUrl,
    createdAt: new Date(media.updatedAt).toISOString()
  };
}

export const localMediaApi = {
  async list(ownerType: MediaOwnerType, ownerId: string): Promise<MediaListResponse> {
    const items = await db.media
      .where('ownerType')
      .equals(ownerType)
      .and(media => media.ownerId === ownerId)
      .toArray();

    return { items: items.map(mediaToDto) };
  },

  async create(data: AddMediaRequest): Promise<MediaDto> {
    const id = crypto.randomUUID();
    const now = Date.now();

    const media: Media = {
      id,
      ownerType: data.ownerType,
      ownerId: data.ownerId,
      mediaType: data.mediaType,
      storageKey: data.storageKey,
      originalFileName: data.originalFileName,
      mimeType: data.mimeType,
      sizeBytes: data.sizeBytes,
      caption: data.caption,
      data: data.data,
      updatedAt: now,
      syncStatus: 'local'
    };

    await db.media.add(media);

    if (data.ownerType === 'zaznam') {
      await queueChange('zaznam', data.ownerId, undefined, 'media', id, 'create', {
        zaznamId: data.ownerId,
        type: data.mediaType,
        storageKey: data.storageKey,
        originalFileName: data.originalFileName,
        mimeType: data.mimeType,
        sizeBytes: data.sizeBytes,
        description: data.caption
      });
    }

    return mediaToDto(media);
  },

  async update(id: string, data: UpdateMediaRequest): Promise<MediaDto> {
    const media = await db.media.get(id);
    if (!media) {
      throw new Error('Media nenalezeno');
    }

    await db.media.update(id, {
      caption: data.caption,
      updatedAt: Date.now()
    });

    const result = await db.media.get(id);
    return mediaToDto(result!);
  },

  async delete(id: string): Promise<void> {
    await db.media.delete(id);
  }
};

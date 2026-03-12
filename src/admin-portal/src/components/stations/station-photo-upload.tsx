"use client";

import { useState, useRef } from "react";
import { Upload, Loader2, X, Star } from "lucide-react";
import { Button } from "@/components/ui/button";
import { stationsApi } from "@/lib/api";
import { useTranslation } from "@/lib/i18n";

export interface PhotoItem {
  id?: string;
  url: string;
  isPrimary?: boolean;
}

interface StationPhotoUploadProps {
  /** Station ID — if provided, photos are persisted to backend on add/remove */
  stationId?: string;
  /** Current photos */
  photos: PhotoItem[];
  /** Called when photos change (for form state) */
  onChange: (photos: PhotoItem[]) => void;
  /** Max number of photos */
  max?: number;
}

const ALLOWED_TYPES = ["image/jpeg", "image/png", "image/gif", "image/webp"];
const MAX_SIZE = 5 * 1024 * 1024; // 5MB

export function StationPhotoUpload({ stationId, photos, onChange, max = 10 }: StationPhotoUploadProps) {
  const { t } = useTranslation();
  const fileInputRef = useRef<HTMLInputElement>(null);
  const [isUploading, setIsUploading] = useState(false);
  const [error, setError] = useState("");

  const handleFileSelect = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const files = e.target.files;
    if (!files || files.length === 0) return;

    setError("");
    setIsUploading(true);

    try {
      const newPhotos: PhotoItem[] = [];

      for (let i = 0; i < files.length; i++) {
        if (photos.length + newPhotos.length >= max) break;

        const file = files[i];
        if (file.size > MAX_SIZE) {
          setError(t("stations.photoSizeError"));
          continue;
        }
        if (!ALLOWED_TYPES.includes(file.type)) {
          setError(t("stations.photoTypeError"));
          continue;
        }

        // Upload to GCS
        const { data } = await stationsApi.uploadPhoto(file);
        const url = data.url;

        // If stationId exists, persist to backend immediately
        if (stationId) {
          const isPrimary = photos.length === 0 && newPhotos.length === 0;
          const { data: photoDto } = await stationsApi.addPhoto(stationId, {
            url,
            isPrimary,
            sortOrder: photos.length + newPhotos.length,
          });
          newPhotos.push({ id: photoDto.id, url: photoDto.url, isPrimary: photoDto.isPrimary });
        } else {
          const isPrimary = photos.length === 0 && newPhotos.length === 0;
          newPhotos.push({ url, isPrimary });
        }
      }

      onChange([...photos, ...newPhotos]);
    } catch {
      setError(t("stations.photoUploadFailed"));
    } finally {
      setIsUploading(false);
      if (fileInputRef.current) fileInputRef.current.value = "";
    }
  };

  const handleRemove = async (index: number) => {
    const photo = photos[index];

    // If persisted, delete from backend
    if (stationId && photo.id) {
      try {
        await stationsApi.removePhoto(stationId, photo.id);
      } catch {
        // continue removing from UI
      }
    }

    const updated = photos.filter((_, i) => i !== index);
    // If we removed the primary, set first remaining as primary
    if (photo.isPrimary && updated.length > 0) {
      updated[0] = { ...updated[0], isPrimary: true };
      if (stationId && updated[0].id) {
        stationsApi.setPrimaryPhoto(stationId, updated[0].id).catch(() => {});
      }
    }
    onChange(updated);
  };

  const handleSetPrimary = async (index: number) => {
    const updated = photos.map((p, i) => ({ ...p, isPrimary: i === index }));
    onChange(updated);

    if (stationId && photos[index].id) {
      try {
        await stationsApi.setPrimaryPhoto(stationId, photos[index].id!);
      } catch {
        // revert on failure
      }
    }
  };

  return (
    <div className="space-y-3">
      <label className="text-sm font-medium">{t("stations.photos")}</label>

      {/* Photo grid */}
      {photos.length > 0 && (
        <div className="grid grid-cols-2 gap-3 sm:grid-cols-3 md:grid-cols-4">
          {photos.map((photo, index) => (
            <div key={photo.id || photo.url} className="group relative aspect-[4/3] overflow-hidden rounded-lg border">
              <img
                src={photo.url}
                alt={`${t("stations.photos")} ${index + 1}`}
                className="h-full w-full object-cover"
              />
              <div className="absolute inset-0 bg-black/0 group-hover:bg-black/30 transition-colors" />
              <div className="absolute right-1 top-1 flex gap-1 opacity-0 group-hover:opacity-100 transition-opacity">
                {!photo.isPrimary && (
                  <button
                    type="button"
                    onClick={() => handleSetPrimary(index)}
                    className="rounded-full bg-white/90 p-1.5 text-amber-500 hover:bg-white shadow-sm"
                    title={t("stations.setPrimary")}
                  >
                    <Star className="h-3.5 w-3.5" />
                  </button>
                )}
                <button
                  type="button"
                  onClick={() => handleRemove(index)}
                  className="rounded-full bg-white/90 p-1.5 text-red-500 hover:bg-white shadow-sm"
                  title={t("common.delete")}
                >
                  <X className="h-3.5 w-3.5" />
                </button>
              </div>
              {photo.isPrimary && (
                <div className="absolute bottom-1 left-1 flex items-center gap-1 rounded bg-amber-500/90 px-1.5 py-0.5 text-[10px] font-medium text-white">
                  <Star className="h-2.5 w-2.5" />
                  {t("stations.primary")}
                </div>
              )}
            </div>
          ))}
        </div>
      )}

      {/* Upload zone */}
      {photos.length < max && (
        <div
          onClick={() => !isUploading && fileInputRef.current?.click()}
          className="flex h-32 cursor-pointer flex-col items-center justify-center gap-2 rounded-lg border-2 border-dashed border-muted-foreground/25 bg-muted/50 hover:border-muted-foreground/50 hover:bg-muted transition-colors"
        >
          {isUploading ? (
            <>
              <Loader2 className="h-8 w-8 animate-spin text-muted-foreground" />
              <span className="text-sm text-muted-foreground">{t("stations.uploading")}</span>
            </>
          ) : (
            <>
              <Upload className="h-8 w-8 text-muted-foreground" />
              <span className="text-sm text-muted-foreground">{t("stations.clickToUploadPhotos")}</span>
              <span className="text-xs text-muted-foreground">{t("stations.photoFormats")}</span>
            </>
          )}
        </div>
      )}

      <input
        ref={fileInputRef}
        type="file"
        accept="image/jpeg,image/png,image/gif,image/webp"
        multiple
        onChange={handleFileSelect}
        className="hidden"
      />

      {error && <p className="text-sm text-red-500">{error}</p>}

      {photos.length > 0 && (
        <p className="text-xs text-muted-foreground">
          {photos.length}/{max} {t("stations.photos").toLowerCase()}
        </p>
      )}
    </div>
  );
}

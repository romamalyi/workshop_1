import { Injectable } from '@angular/core';

const STORAGE_KEY = 'leohr.celebratedPromotionIds';

// Browser-local, so another browser or device can show the celebration once more.
@Injectable({ providedIn: 'root' })
export class CelebrationSeenStore {
  // Promotion.id is typed string but the API sends a number, so ids are compared as strings.
  has(promotionId: string): boolean {
    return this.read().includes(String(promotionId));
  }

  add(promotionId: string): void {
    try {
      localStorage.setItem(STORAGE_KEY, JSON.stringify([...this.read(), String(promotionId)]));
    } catch {
      // Storage unavailable: the celebration may show again, which is harmless.
    }
  }

  private read(): string[] {
    try {
      const ids: unknown = JSON.parse(localStorage.getItem(STORAGE_KEY) ?? '[]');
      return Array.isArray(ids) ? ids.map(String) : [];
    } catch {
      return [];
    }
  }
}

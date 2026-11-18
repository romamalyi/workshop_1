import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { CreatePromotionCriteriaRequest, PromotionCriteria, UpdatePromotionCriteriaRequest } from '../promotion-criteria.model';

@Injectable({ providedIn: 'root' })
export class PromotionCriteriaService {
  constructor(private http: HttpClient) {}

  getAll(technology?: string, includeInactive = false): Observable<PromotionCriteria[]> {
    let params = new HttpParams().set('includeInactive', includeInactive);
    if (technology) params = params.set('technology', technology);
    return this.http.get<PromotionCriteria[]>('/api/promotion-criteria', { params });
  }

  getById(id: number): Observable<PromotionCriteria> {
    return this.http.get<PromotionCriteria>(`/api/promotion-criteria/${id}`);
  }

  create(request: CreatePromotionCriteriaRequest): Observable<PromotionCriteria> {
    return this.http.post<PromotionCriteria>('/api/promotion-criteria', request);
  }

  update(id: number, request: UpdatePromotionCriteriaRequest, mode: 'Soft' | 'Hard' = 'Soft'): Observable<PromotionCriteria> {
    return this.http.put<PromotionCriteria>(`/api/promotion-criteria/${id}?mode=${mode}`, request);
  }

  reactivate(id: number): Observable<PromotionCriteria> {
    return this.http.patch<PromotionCriteria>(`/api/promotion-criteria/${id}/reactivate`, null);
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`/api/promotion-criteria/${id}`);
  }
}

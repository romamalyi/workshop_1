import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import {
  Activity,
  ActivityCategory,
  CategorizedActivity,
  CreateActivityRequest,
  UpdateActivityRequest,
} from '../activity.model';

@Injectable({ providedIn: 'root' })
export class ActivitiesService {
  constructor(private http: HttpClient) {}

  getAll(category?: ActivityCategory, includeInactive = false): Observable<Activity[]> {
    let params = new HttpParams().set('includeInactive', includeInactive);
    if (category) params = params.set('category', category);
    return this.http.get<Activity[]>('/api/activities', { params });
  }

  getGrouped(): Observable<CategorizedActivity[]> {
    return this.http.get<CategorizedActivity[]>('/api/activities/group');
  }

  getById(id: number): Observable<Activity> {
    return this.http.get<Activity>(`/api/activities/${id}`);
  }

  create(request: CreateActivityRequest): Observable<Activity> {
    return this.http.post<Activity>('/api/activities', request);
  }

  update(id: number, request: UpdateActivityRequest, mode: 'Soft' | 'Hard' = 'Soft'): Observable<Activity> {
    return this.http.put<Activity>(`/api/activities/${id}?mode=${mode}`, request);
  }

  reactivate(id: number): Observable<Activity> {
    return this.http.patch<Activity>(`/api/activities/${id}/reactivate`, null);
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`/api/activities/${id}`);
  }
}

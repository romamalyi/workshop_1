import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import {
  OnboardingType,
  OnboardingTypeDetail,
  OnboardingTypes,
  SaveTypeRequest,
  TypeLibraryTemplate,
} from '../models/onboarding-type.model';

@Injectable({
  providedIn: 'root',
})
export class OnboardingTypesService {
  private readonly http = inject(HttpClient);
  private readonly endpoint = '/api/onboarding/types';

  getAll(): Observable<OnboardingTypes> {
    return this.http.get<OnboardingTypes>(this.endpoint);
  }

  getLibrary(): Observable<TypeLibraryTemplate[]> {
    return this.http.get<TypeLibraryTemplate[]>(`${this.endpoint}/library`);
  }

  getById(id: number): Observable<OnboardingTypeDetail> {
    return this.http.get<OnboardingTypeDetail>(`${this.endpoint}/${id}`);
  }

  create(request: SaveTypeRequest): Observable<OnboardingType> {
    return this.http.post<OnboardingType>(this.endpoint, request);
  }

  update(id: number, request: SaveTypeRequest): Observable<OnboardingType> {
    return this.http.put<OnboardingType>(`${this.endpoint}/${id}`, request);
  }

  duplicate(id: number): Observable<OnboardingType> {
    return this.http.post<OnboardingType>(`${this.endpoint}/${id}/duplicate`, null);
  }

  setArchived(id: number, isArchived: boolean): Observable<OnboardingType> {
    return this.http.put<OnboardingType>(`${this.endpoint}/${id}/archived`, { isArchived });
  }
}

import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import {
  KnowledgeEvaluation,
  RequestKnowledgeEvaluation,
  UpdateKnowledgeEvaluation,
} from '../models/knowledge-evaluation.model';

@Injectable({ providedIn: 'root' })
export class KnowledgeEvaluationsService {
  private readonly http = inject(HttpClient);

  getAll(): Observable<KnowledgeEvaluation[]> {
    return this.http.get<KnowledgeEvaluation[]>('/api/knowledge-evaluations');
  }

  getLatest(promotionId: number): Observable<KnowledgeEvaluation | null> {
    return this.http.get<KnowledgeEvaluation | null>(`/api/knowledge-evaluations/promotion/${promotionId}`);
  }

  create(promotionId: number, request: RequestKnowledgeEvaluation): Observable<KnowledgeEvaluation> {
    return this.http.post<KnowledgeEvaluation>(`/api/knowledge-evaluations/promotion/${promotionId}`, request);
  }

  updateRequest(id: number, request: RequestKnowledgeEvaluation): Observable<KnowledgeEvaluation> {
    return this.http.put<KnowledgeEvaluation>(`/api/knowledge-evaluations/${id}/request`, request);
  }

  update(id: number, request: UpdateKnowledgeEvaluation): Observable<KnowledgeEvaluation> {
    return this.http.patch<KnowledgeEvaluation>(`/api/knowledge-evaluations/${id}`, request);
  }
}

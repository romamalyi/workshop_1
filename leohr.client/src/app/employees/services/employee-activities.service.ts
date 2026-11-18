import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { EmployeeActivityStatus } from 'src/app/shared/models/employee-activity-status.model';
import {
  CreateEmployeeActivityRequest,
  EmployeeActivity,
  SelfReportRequest,
  UpdateEmployeeActivityRequest,
} from '../models/employee-activity.model';

@Injectable({ providedIn: 'root' })
export class EmployeeActivitiesService {
  private readonly http = inject(HttpClient);

  getAll(employeeId: string, status?: EmployeeActivityStatus): Observable<EmployeeActivity[]> {
    let params = new HttpParams();
    if (status) params = params.set('status', status);
    return this.http.get<EmployeeActivity[]>(`/api/employees/${employeeId}/activities`, { params });
  }

  getById(employeeId: string, id: number): Observable<EmployeeActivity> {
    return this.http.get<EmployeeActivity>(`/api/employees/${employeeId}/activities/${id}`);
  }

  create(employeeId: string, request: CreateEmployeeActivityRequest): Observable<EmployeeActivity> {
    return this.http.post<EmployeeActivity>(`/api/employees/${employeeId}/activities`, request);
  }

  update(employeeId: string, id: number, request: UpdateEmployeeActivityRequest): Observable<EmployeeActivity> {
    return this.http.put<EmployeeActivity>(`/api/employees/${employeeId}/activities/${id}`, request);
  }

  selfReport(employeeId: string, id: number, request: SelfReportRequest): Observable<EmployeeActivity> {
    return this.http.put<EmployeeActivity>(`/api/employees/${employeeId}/activities/${id}/self-report`, request);
  }

  approve(employeeId: string, id: number, accomplishedOn: string, approvedById?: string): Observable<EmployeeActivity> {
    return this.http.put<EmployeeActivity>(`/api/employees/${employeeId}/activities/${id}/approve`, {
      approvedById: approvedById ?? null,
      accomplishedOn,
    });
  }

  delete(employeeId: string, id: number): Observable<void> {
    return this.http.delete<void>(`/api/employees/${employeeId}/activities/${id}`);
  }
}

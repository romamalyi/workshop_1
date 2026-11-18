import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { EmployeeFilterModel } from 'src/app/core/models/base-filter.model';
import { PagedResponse } from 'src/app/shared/models/paged-response.model';
import { Employee, EmployeeFilterOptions, EmployeeHistory, EmployeeProjectsDto } from '../models/employee.model';
import { EmployeeHistorySettings } from '../models/employee-history-settings.model';

@Injectable({
  providedIn: 'root',
})
export class EmployeesService {
  constructor(private http: HttpClient) {}

  getActiveEmployees(): Observable<Employee[]> {
    return this.http.get<Employee[]>('/api/employees');
  }

  search(filter: EmployeeFilterModel | null = null): Observable<PagedResponse<Employee>> {
    let params = new HttpParams();

    if (filter) {
      if (filter.searchTerm) params = params.set('searchTerm', filter.searchTerm);
      if (filter.pageNumber) params = params.set('pageNumber', filter.pageNumber.toString());
      if (filter.pageSize) params = params.set('pageSize', filter.pageSize.toString());
      filter.seniorities?.forEach((v) => (params = params.append('seniorities', v)));
      filter.profiles?.forEach((v) => (params = params.append('profiles', v)));
      filter.projects?.forEach((v) => (params = params.append('projects', v)));
      filter.hrManagers?.forEach((v) => (params = params.append('hrManagers', v)));
    }

    return this.http.get<PagedResponse<Employee>>('/api/employees/search', { params });
  }

  getFilterOptions(): Observable<EmployeeFilterOptions> {
    return this.http.get<EmployeeFilterOptions>('/api/employees/filter-options');
  }

  getEmployeeById(id: string): Observable<Employee> {
    return this.http.get<Employee>(`/api/employees/${id}`);
  }

  getEmployeeHistory(id: string, userId?: string): Observable<EmployeeHistory[]> {
    const params = userId ? new HttpParams().set('userId', userId) : undefined;
    return this.http.get<EmployeeHistory[]>(`/api/employees/${id}/history`, { params });
  }

  getEmployeesProjects(ids: string[]): Observable<EmployeeProjectsDto[]> {
    let params = new HttpParams();
    ids.forEach((id) => {
      params = params.append('userIds', id);
    });
    return this.http.get<EmployeeProjectsDto[]>('/api/employees/projects', { params });
  }

  getSeniorities(): Observable<string[]> {
    return this.http.get<string[]>('/api/employees/seniorities');
  }

  updatePdpUrl(id: string, pdpUrl: string | null): Observable<void> {
    return this.http.put<void>(`/api/employees/${id}/pdp-url`, { pdpUrl });
  }

  getHistorySettings(): Observable<EmployeeHistorySettings> {
    return this.http.get<EmployeeHistorySettings>('/api/settings/employee-history');
  }

  saveHistorySettings(settings: EmployeeHistorySettings): Observable<void> {
    return this.http.put<void>('/api/settings/employee-history', settings);
  }
}

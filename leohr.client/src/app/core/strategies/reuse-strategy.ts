import { ActivatedRouteSnapshot, DetachedRouteHandle, RouteReuseStrategy } from '@angular/router';
import { EmployeesPageComponent } from '../../employees/components/employees-page/employees-page.component';

export class AppReuseStrategy implements RouteReuseStrategy {
  private handle: DetachedRouteHandle | null = null;

  shouldDetach(route: ActivatedRouteSnapshot): boolean {
    return route.component === EmployeesPageComponent;
  }

  store(route: ActivatedRouteSnapshot, handle: DetachedRouteHandle | null): void {
    if (route.component === EmployeesPageComponent) {
      this.handle = handle;
    }
  }

  shouldAttach(route: ActivatedRouteSnapshot): boolean {
    return route.component === EmployeesPageComponent && this.handle !== null;
  }

  retrieve(route: ActivatedRouteSnapshot): DetachedRouteHandle | null {
    return route.component === EmployeesPageComponent ? this.handle : null;
  }

  shouldReuseRoute(future: ActivatedRouteSnapshot, curr: ActivatedRouteSnapshot): boolean {
    return future.routeConfig === curr.routeConfig;
  }
}

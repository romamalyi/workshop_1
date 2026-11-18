import { inject, Injectable } from '@angular/core';
import { ToastrService } from 'ngx-toastr';

@Injectable({
  providedIn: 'root',
})
export class ToastService {
  private toastr = inject(ToastrService);

  success(message: string): void {
    this.toastr.success(message, 'Success', { timeOut: 3000 });
  }

  warning(message: string): void {
    this.toastr.warning(message, 'Needs attention', { timeOut: 8000, closeButton: true });
  }

  error(message: string): void {
    this.toastr.error(message, 'Error', { timeOut: 5000, closeButton: true });
  }
}

import { ChangeDetectionStrategy, Component, HostListener, output } from '@angular/core';

@Component({
  selector: 'app-celebration-video',
  templateUrl: './celebration-video.component.html',
  standalone: false,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CelebrationVideoComponent {
  public readonly closed = output<void>();

  @HostListener('document:keydown.escape')
  close(): void {
    this.closed.emit();
  }
}

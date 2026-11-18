import { DestroyRef, Directive, ElementRef, inject, output } from '@angular/core';

// Emits when the element scrolls near the viewport. It reports a change only, so re-create the element to ask again.
@Directive({
  selector: '[appInView]',
  standalone: true,
})
export class InViewDirective {
  readonly appInView = output<void>();

  constructor() {
    const element = inject<ElementRef<HTMLElement>>(ElementRef).nativeElement;
    const observer = new IntersectionObserver(
      (entries) => {
        if (entries.some((entry) => entry.isIntersecting)) this.appInView.emit();
      },
      { rootMargin: '400px' },
    );
    observer.observe(element);
    inject(DestroyRef).onDestroy(() => observer.disconnect());
  }
}

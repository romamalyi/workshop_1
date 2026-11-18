import { CdkVirtualScrollViewport } from '@angular/cdk/scrolling';
import { DestroyRef, Directive, ElementRef, NgZone, inject } from '@angular/core';

// Sizes a scrolling element (e.g. a virtual-scroll table) to end at the bottom of the area that scrolls around
// it (the navigation's content column), so only the element scrolls, not that area too.
// It follows the space above it (navigation, filters that wrap), so no header height is hard-coded.
// A CSS `min-height` still applies on short windows.
@Directive({
  selector: '[appFitToWindow]',
  standalone: true,
})
export class FitToWindowDirective {
  constructor() {
    const element = inject<ElementRef<HTMLElement>>(ElementRef).nativeElement;
    const viewport = inject(CdkVirtualScrollViewport, { self: true, optional: true });
    const zone = inject(NgZone);
    const destroyRef = inject(DestroyRef);

    // The navigation's content column scrolls (styles.css); the window is the fallback if that ever changes.
    // A closer `overflow-x: auto` wrapper is not it: that only makes its overflow-y `auto` on paper, while it
    // still grows with its content.
    const scrollContainer = (): HTMLElement => {
      const content = element.closest<HTMLElement>('lbt-navigation .content');
      return content && getComputedStyle(content).overflowY === 'auto' ? content : document.documentElement;
    };

    let frame = 0;

    // Refits on every change, also below the element (e.g. a footer that appears after data loads).
    // It does not loop: once the height is right, rewriting the same value fires no resize.
    const fit = () => {
      frame = 0;
      const container = scrollContainer();
      const containerTop = container === document.documentElement ? 0 : container.getBoundingClientRect().top;
      // Measured from the start of the scrolled content, so the result does not depend on the scroll position.
      const top = element.getBoundingClientRect().top - containerTop + container.scrollTop;
      const visibleHeight = container.clientHeight;

      // Take all the room down to the bottom of the visible area, then give back whatever now overflows it
      // (padding and content below the element), so the scrolled content ends exactly there.
      const before = element.style.height;
      element.style.height = `${visibleHeight - top}px`;
      const overflow = Math.max(0, container.scrollHeight - visibleHeight);
      // Rounded down: a fractional top would leave the content a sub-pixel taller than the area, i.e. scrollable.
      element.style.height = `${Math.floor(visibleHeight - top - overflow)}px`;
      if (element.style.height !== before) zone.run(() => viewport?.checkViewportSize());
    };
    const schedule = () => {
      if (!frame) frame = requestAnimationFrame(fit);
    };

    zone.runOutsideAngular(() => {
      window.addEventListener('resize', schedule);
      // Fires when content around the element changes size, e.g. filters wrapping or data loading.
      const observer = new ResizeObserver(schedule);
      observer.observe(document.body);
      if (element.parentElement) observer.observe(element.parentElement);
      schedule();
      destroyRef.onDestroy(() => {
        window.removeEventListener('resize', schedule);
        observer.disconnect();
        cancelAnimationFrame(frame);
      });
    });
  }
}

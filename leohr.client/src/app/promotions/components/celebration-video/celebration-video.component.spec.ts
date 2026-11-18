import { ComponentFixture, TestBed } from '@angular/core/testing';
import { CelebrationVideoComponent } from './celebration-video.component';

describe('CelebrationVideoComponent', () => {
  let fixture: ComponentFixture<CelebrationVideoComponent>;
  let closed: ReturnType<typeof vi.fn<() => void>>;

  beforeEach(() => {
    TestBed.configureTestingModule({ declarations: [CelebrationVideoComponent] });
    fixture = TestBed.createComponent(CelebrationVideoComponent);
    closed = vi.fn<() => void>();
    fixture.componentInstance.closed.subscribe(closed);
    fixture.detectChanges();
  });

  const video = (): HTMLVideoElement => fixture.nativeElement.querySelector('video');

  it('plays muted and without controls', () => {
    expect(video().muted).toBe(true);
    expect(video().autoplay).toBe(true);
    expect(video().controls).toBe(false);
  });

  it('closes when the video ends', () => {
    video().dispatchEvent(new Event('ended'));
    expect(closed).toHaveBeenCalledOnce();
  });

  it('closes when the video fails to load', () => {
    video().dispatchEvent(new Event('error'));
    expect(closed).toHaveBeenCalledOnce();
  });

  it('closes from the close button', () => {
    fixture.nativeElement.querySelector('button').click();
    expect(closed).toHaveBeenCalledOnce();
  });

  it('closes on Escape', () => {
    document.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape' }));
    expect(closed).toHaveBeenCalledOnce();
  });
});

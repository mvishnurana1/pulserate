import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { Subject } from 'rxjs';
import { App } from './app';
import { RealtimeService } from './core/realtime/realtime.service';

class FakeRealtimeService {
  readonly status = signal<'connected'>('connected');
  readonly resultRecorded = new Subject();
  readonly wentDown = new Subject();
  readonly recovered = new Subject();
  start = vi.fn().mockResolvedValue(undefined);
}

describe('App shell', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [App],
      providers: [
        provideRouter([]),
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: RealtimeService, useClass: FakeRealtimeService },
      ],
    }).compileComponents();
  });

  it('starts the live connection', () => {
    TestBed.createComponent(App);
    const realtime = TestBed.inject(RealtimeService) as unknown as FakeRealtimeService;
    expect(realtime.start).toHaveBeenCalled();
  });

  it('renders navigation and the live indicator', async () => {
    const fixture = TestBed.createComponent(App);
    await fixture.whenStable();
    const element = fixture.nativeElement as HTMLElement;

    expect(element.querySelector('.brand')?.textContent).toContain('PulseBoard');
    expect(element.querySelectorAll('.nav__link').length).toBe(2);
    expect(element.querySelector('.connection')?.textContent).toContain('Live');
  });
});

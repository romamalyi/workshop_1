import { CelebrationSeenStore } from './celebration-seen.store';

describe('CelebrationSeenStore', () => {
  beforeEach(() => localStorage.clear());

  it('remembers celebrated promotions', () => {
    const store = new CelebrationSeenStore();
    store.add('1');
    store.add('2');
    expect(store.has('1')).toBe(true);
    expect(store.has('2')).toBe(true);
    expect(store.has('3')).toBe(false);
  });

  it('matches a numeric id from the API against the stored one', () => {
    const apiId = 7 as unknown as string;
    const store = new CelebrationSeenStore();
    store.add(apiId);
    expect(new CelebrationSeenStore().has(apiId)).toBe(true);
  });

  it('treats unreadable storage as nothing celebrated', () => {
    localStorage.setItem('leohr.celebratedPromotionIds', 'not json');
    expect(new CelebrationSeenStore().has('1')).toBe(false);
  });
});

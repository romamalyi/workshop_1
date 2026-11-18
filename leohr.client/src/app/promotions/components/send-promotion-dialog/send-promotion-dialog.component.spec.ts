import { NO_ERRORS_SCHEMA } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { EmailRecipients } from 'src/app/core/services/email-templates.service';
import { Employee } from 'src/app/employees/models/employee.model';
import { EmployeesService } from 'src/app/employees/services/employees.service';
import { PromotionsService } from '../../services/promotions.service';
import { SendPromotionDialogComponent } from './send-promotion-dialog.component';

const recipients: EmailRecipients = {
  to: [
    { name: 'Ann Lee', email: 'ann@x.com', roles: ['Employee'] },
    { name: 'Hal Roe', email: 'hal@x.com', roles: ['HrManager', 'PromotionHolder'] },
  ],
  cc: [{ name: 'Cid Poe', email: 'cid@x.com', roles: ['HrManagersManager'] }],
  unresolved: ['ProjectManager'],
};

const employee = (id: string, firstName: string, email: string): Employee =>
  ({ id, firstName, lastName: 'Test', email }) as Employee;

const employees = [
  employee('1', 'Ann', 'ann@x.com'),
  employee('2', 'Cid', 'CID@x.com'),
  employee('3', 'Dee', 'dee@x.com'),
  employee('4', 'Eve', 'eve@x.com'),
  employee('5', 'Fay', ''),
];

describe('SendPromotionDialogComponent', () => {
  let fixture: ComponentFixture<SendPromotionDialogComponent>;
  let component: SendPromotionDialogComponent;
  let confirmed: ReturnType<typeof vi.fn<(ids: string[]) => void>>;

  const create = (recipientsResponse = of(recipients)): void => {
    TestBed.configureTestingModule({
      declarations: [SendPromotionDialogComponent],
      providers: [
        { provide: PromotionsService, useValue: { getEmailRecipients: vi.fn(() => recipientsResponse) } },
        { provide: EmployeesService, useValue: { getActiveEmployees: vi.fn(() => of(employees)) } },
      ],
      schemas: [NO_ERRORS_SCHEMA],
    });
    fixture = TestBed.createComponent(SendPromotionDialogComponent);
    component = fixture.componentInstance;
    fixture.componentRef.setInput('promotionId', 7);
    fixture.componentRef.setInput('subject', 'Subject');
    fixture.componentRef.setInput('body', 'Body');
    confirmed = vi.fn<(ids: string[]) => void>();
    component.confirmed.subscribe(confirmed);
    fixture.detectChanges();
  };

  const text = (): string => fixture.nativeElement.textContent;
  const removeButtons = (): HTMLButtonElement[] =>
    Array.from(fixture.nativeElement.querySelectorAll('button[aria-label^="Remove"]'));

  it('lists To and CC with their roles, and the roles no one was found for', () => {
    create();
    const chips = Array.from<HTMLElement>(fixture.nativeElement.querySelectorAll('.recipient-chip')).map((chip) =>
      chip.getAttribute('aria-label'),
    );
    expect(chips).toContain('Ann Lee, ann@x.com, Employee');
    expect(chips).toContain('Hal Roe, hal@x.com, HR manager, Promotion holder');
    expect(chips).toContain("Cid Poe, cid@x.com, HR manager's manager");
    expect(text()).toContain("Project manager: no one found, so this role won't receive the email.");
  });

  it('offers only people with an email who are not on the email yet', () => {
    create();
    expect(component.ccCandidates().map((c) => c.id)).toEqual(['3', '4']);

    component.addCc({ id: '3', label: 'Dee Test' });
    expect(component.ccCandidates().map((c) => c.id)).toEqual(['4']);
  });

  it('lets HR remove only the people they added', () => {
    create();
    expect(removeButtons()).toHaveLength(0);

    component.addCc({ id: '3', label: 'Dee Test' });
    fixture.detectChanges();
    expect(removeButtons()).toHaveLength(1);

    removeButtons()[0].click();
    fixture.detectChanges();
    expect(text()).not.toContain('Dee Test');
  });

  it('sends the added people once the level is confirmed', () => {
    create();
    component.addCc({ id: '3', label: 'Dee Test' });
    component.addCc({ id: '4', label: 'Eve Test' });

    component.send();
    expect(confirmed).not.toHaveBeenCalled();

    component.levelConfirmed.set(true);
    component.send();
    expect(confirmed).toHaveBeenCalledWith(['3', '4']);
  });

  it("can't send when the recipients fail to load", () => {
    create(throwError(() => new Error('down')));
    component.levelConfirmed.set(true);

    expect(text()).toContain("The recipients couldn't be loaded");
    expect(component.canSend()).toBe(false);
  });
});

import {
  EditTestResultsComponent,
  RESULT_PARAM_INPUT_CLASS
} from './edit-test-results.component';

describe('EditTestResultsComponent Enter-key parameter navigation', () => {
  let root: HTMLDivElement;
  let saveClicked: boolean;

  function makeInput(opts?: { readonly?: boolean; disabled?: boolean; hidden?: boolean; value?: string }): HTMLInputElement {
    const input = document.createElement('input');
    input.type = 'text';
    input.className = `form-control input-sm ${RESULT_PARAM_INPUT_CLASS}`;
    input.value = opts && opts.value != null ? opts.value : '';
    if (opts && opts.readonly) {
      input.readOnly = true;
    }
    if (opts && opts.disabled) {
      input.disabled = true;
    }
    if (opts && opts.hidden) {
      input.style.display = 'none';
    }
    root.appendChild(input);
    return input;
  }

  beforeEach(() => {
    saveClicked = false;
    root = document.createElement('div');
    root.className = 'edit-test-results-page';
    document.body.appendChild(root);
    const saveBtn = document.createElement('button');
    saveBtn.type = 'button';
    saveBtn.id = 'save-results';
    saveBtn.addEventListener('click', () => { saveClicked = true; });
    root.appendChild(saveBtn);
  });

  afterEach(() => {
    if (root && root.parentNode) {
      root.parentNode.removeChild(root);
    }
  });

  it('ENTER on first parameter focuses second parameter', () => {
    const a = makeInput({ value: '14.1' });
    const b = makeInput({ value: '41.8' });
    makeInput({ value: '4.42' });
    const moved = EditTestResultsComponent.focusNextEditableParameterInput(a, root);
    expect(moved).toBe(true);
    expect(document.activeElement).toBe(b);
    expect(a.value).toBe('14.1');
  });

  it('ENTER on middle parameter focuses next parameter', () => {
    makeInput();
    const mid = makeInput({ value: '94.6' });
    const next = makeInput();
    const moved = EditTestResultsComponent.focusNextEditableParameterInput(mid, root);
    expect(moved).toBe(true);
    expect(document.activeElement).toBe(next);
    expect(mid.value).toBe('94.6');
  });

  it('ENTER on final parameter does not move focus and does not click Save', () => {
    makeInput();
    const last = makeInput({ value: '1.2' });
    last.focus();
    const moved = EditTestResultsComponent.focusNextEditableParameterInput(last, root);
    expect(moved).toBe(false);
    expect(document.activeElement).toBe(last);
    expect(saveClicked).toBe(false);
    expect(last.value).toBe('1.2');
  });

  it('skips disabled parameter', () => {
    const a = makeInput();
    makeInput({ disabled: true });
    const c = makeInput();
    const moved = EditTestResultsComponent.focusNextEditableParameterInput(a, root);
    expect(moved).toBe(true);
    expect(document.activeElement).toBe(c);
  });

  it('skips readonly parameter', () => {
    const a = makeInput();
    makeInput({ readonly: true });
    const c = makeInput();
    const moved = EditTestResultsComponent.focusNextEditableParameterInput(a, root);
    expect(moved).toBe(true);
    expect(document.activeElement).toBe(c);
  });

  it('skips hidden / non-rendered parameter', () => {
    const a = makeInput();
    makeInput({ hidden: true });
    const c = makeInput();
    const moved = EditTestResultsComponent.focusNextEditableParameterInput(a, root);
    expect(moved).toBe(true);
    expect(document.activeElement).toBe(c);
  });

  it('works dynamically for many parameters', () => {
    const inputs: HTMLInputElement[] = [];
    for (let i = 0; i < 12; i++) {
      inputs.push(makeInput({ value: String(i) }));
    }
    for (let i = 0; i < 11; i++) {
      const moved = EditTestResultsComponent.focusNextEditableParameterInput(inputs[i], root);
      expect(moved).toBe(true);
      expect(document.activeElement).toBe(inputs[i + 1]);
      expect(inputs[i].value).toBe(String(i));
    }
    expect(EditTestResultsComponent.focusNextEditableParameterInput(inputs[11], root)).toBe(false);
  });

  it('single parameter ENTER is safe (no next, no save)', () => {
    const only = makeInput({ value: '7' });
    only.focus();
    expect(EditTestResultsComponent.focusNextEditableParameterInput(only, root)).toBe(false);
    expect(document.activeElement).toBe(only);
    expect(saveClicked).toBe(false);
    expect(only.value).toBe('7');
  });

  it('isEditableParameterInput rejects disabled and readonly', () => {
    const ok = makeInput();
    const ro = makeInput({ readonly: true });
    const dis = makeInput({ disabled: true });
    expect(EditTestResultsComponent.isEditableParameterInput(ok)).toBe(true);
    expect(EditTestResultsComponent.isEditableParameterInput(ro)).toBe(false);
    expect(EditTestResultsComponent.isEditableParameterInput(dis)).toBe(false);
  });

  it('does not treat non-parameter inputs as navigation targets', () => {
    const search = document.createElement('input');
    search.type = 'text';
    search.className = 'form-control';
    root.insertBefore(search, root.firstChild);
    const a = makeInput();
    const b = makeInput();
    EditTestResultsComponent.focusNextEditableParameterInput(a, root);
    expect(document.activeElement).toBe(b);
    expect(document.activeElement).not.toBe(search);
  });

  it('onParameterEnterKey prevents default and does not trigger save button', () => {
    const a = makeInput({ value: '10' });
    const b = makeInput();
    const host = { nativeElement: root } as any;
    const component = Object.create(EditTestResultsComponent.prototype) as EditTestResultsComponent;
    (component as any).host = host;

    let prevented = false;
    const event = {
      target: a,
      preventDefault: () => { prevented = true; }
    } as any;

    component.onParameterEnterKey(event);
    expect(prevented).toBe(true);
    expect(document.activeElement).toBe(b);
    expect(saveClicked).toBe(false);
    expect(a.value).toBe('10');
  });
});

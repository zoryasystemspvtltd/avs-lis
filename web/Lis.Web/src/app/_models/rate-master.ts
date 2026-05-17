export class RateMaster {
  id?: number;
  rate: number;
  effectiveStart: Date;
  effectiveEnd: Date;
  isActive: boolean = true;
  testId: number;
  testName?: string;
}

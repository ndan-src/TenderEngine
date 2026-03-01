export interface Tender {
  TenderID: number;

  // Identity
  SourceId: string;
  LotId?: string;
  NoticeId?: string;
  NoticeVersion?: string;
  NoticeType?: string;
  NoticeStatus?: 'Active' | 'Amendment' | 'Awarded' | null;

  // Title
  TitleDe?: string;
  TitleEn?: string;

  // Description
  DescriptionDe?: string;
  DescriptionEn?: string;

  // Buyer
  BuyerName?: string;
  BuyerNameEn?: string;
  BuyerWebsite?: string;
  BuyerContactEmail?: string;
  BuyerContactPhone?: string;
  BuyerCity?: string;
  BuyerCountry?: string;

  // Classification
  CpvCode?: string;
  AdditionalCpvCodes?: string;
  NutsCode?: string;
  ContractNature?: string;
  ProcedureType?: string;

  // Financials
  ValueEuro?: number;

  // Dates
  PublicationDate?: string;
  SubmissionDeadline?: string;
  ContractStartDate?: string;
  ContractEndDate?: string;
  Deadline?: string;

  // Portal
  BuyerPortalUrl?: string;

  // AI Analysis
  SuitabilityScore?: number;
  EnglishExecutiveSummary?: string;
  FatalFlaws?: string;
  HardCertifications?: string;
  TechStack?: string;
  EligibilityProbability?: number;

  // Meta
  RawXml?: string;
  CreatedAt: string;
}


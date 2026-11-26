export type OpportunityType = 'ChairRental' | 'StaffVacancy';
export type WorkspaceType =
  | 'StylingChair'
  | 'BarberChair'
  | 'NailStation'
  | 'MakeupStation'
  | 'TreatmentRoom'
  | 'MassageRoom'
  | 'TattooStation'
  | 'EntireStudio'
  | 'Other';
export type OpportunityStatus = 'Draft' | 'Published' | 'Filled' | 'Paused' | 'Closed';
export type RatePeriod = 'Hour' | 'Day' | 'Week' | 'Month' | 'RevenueShare' | 'Negotiable';

export interface SpaceOpportunity {
  id: string;
  businessId: string;
  businessName: string;
  city: string;
  province: string;
  workspaceType: WorkspaceType;
  opportunityType: OpportunityType;
  status: OpportunityStatus;
  title: string;
  description: string;
  requiredSkills: string;
  includedAmenities: string;
  houseRules: string;
  spacesAvailable: number;
  rateAmount: number | null;
  ratePeriod: RatePeriod;
  startDate: string;
  endDate: string | null;
  recurring: boolean;
  bringOwnClients: boolean;
  approvalRequired: boolean;
  imageUrl: string | null;
  imageUrls: string[];
  applicationCount: number;
  createdAtUtc: string;
  isVerifiedParty: boolean;
  isDemo: boolean;
  isExpired: boolean;
  isUrgent: boolean;
  distanceKm: number | null;
  isSaved: boolean;
}

export interface SpaceApplication {
  id: string;
  opportunityId: string;
  opportunityTitle: string;
  applicantUserId: string;
  applicantName: string;
  applicantEmail: string | null;
  message: string;
  profileSummary: string;
  skills: string;
  availability: string;
  portfolioUrl: string | null;
  status: ApplicationStatus;
  createdAtUtc: string;
  updatedAtUtc: string;
  timeline: { status: ApplicationStatus; changedAtUtc: string }[];
}

export type ApplicationStatus = 'Submitted' | 'Viewed' | 'Accepted' | 'Declined' | 'Withdrawn' | 'Filled';

export interface SpaceApplicantProfile {
  summary: string;
  skills: string;
  availability: string;
  availableFrom: string | null;
  portfolioUrl: string | null;
  preferredCities: string[];
  preferredOpportunityTypes: OpportunityType[];
  matchingAlertsEnabled: boolean;
  updatedAtUtc: string;
}

export interface MatchedOpportunity {
  opportunity: SpaceOpportunity;
  matchScore: number;
  matchReasons: string[];
}

export const workspaceLabels: Record<WorkspaceType, string> = {
  StylingChair: 'Styling chair',
  BarberChair: 'Barber chair',
  NailStation: 'Nail station',
  MakeupStation: 'Makeup station',
  TreatmentRoom: 'Treatment room',
  MassageRoom: 'Massage room',
  TattooStation: 'Tattoo station',
  EntireStudio: 'Entire studio',
  Other: 'Other workspace',
};

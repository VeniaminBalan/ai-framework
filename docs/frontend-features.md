# Frontend Features Specification

## Overview
This document outlines the frontend features for the PatientSyncHealth medical practice application. The system supports two user roles: **Doctor** and **Nurse**, each with specific workflows and capabilities.

### Permission Model
- **Doctors**: Can manage their own calendar and appointments only
- **Nurses**: Are attributed to one or more doctors and can only view/manage patients and appointments for their assigned doctors
- A nurse without doctor attributions has no access to scheduling features

---

## 1. Authentication

### 1.1 Login Page
- **Route**: `/login`
- **Description**: Single login page for both Doctors and Nurses
- **Authentication**: Keycloak OIDC integration via `react-oidc-context`

**Features**:
- Email/username and password fields
- "Remember me" checkbox
- Forgot password link (redirects to Keycloak)
- Role-based redirect after login:
  - Doctors → `/doctor/calendar`
  - Nurses → `/nurse/patients`

**UI Components**:
- Login form with validation
- Loading state during authentication
- Error message display for failed attempts

---

## 2. Doctor Features

### 2.1 Today's Calendar (Appointments)
- **Route**: `/doctor/calendar`
- **Description**: View and manage today's scheduled appointments

**Features**:
- Daily calendar view showing all appointments
- Time slots displayed in 15-minute increments
- Appointment cards showing:
  - Patient name
  - Appointment time and duration
  - Purpose (Examination, FollowUp, Consultation, Checkup, Emergency)
  - Status indicator (Scheduled, Confirmed, InProgress, Completed, Cancelled)
- Filter by appointment status
- Quick navigation to previous/next day

**Appointment Actions**:
- **Confirm**: Mark scheduled appointment as confirmed
- **Start**: Begin the appointment (changes status to InProgress)
- **Complete**: Finish the appointment
- **Cancel**: Cancel with optional reason
- **Start Examination**: Create examination from appointment

### 2.2 Start Examination from Appointment
- **Route**: `/doctor/examination/new?appointmentId={id}`
- **Description**: Create an examination record linked to an appointment

**Flow**:
1. Doctor clicks "Start Examination" on an appointment
2. System pre-fills patient and doctor information
3. Doctor enters:
   - Diagnosis (optional, can be added later)
   - Notes
4. On save:
   - Creates Examination record
   - Links examination to appointment (ResultingExaminationId)
   - Updates appointment status to Completed
   - Updates patient's LastExaminationDate and NextExaminationDate

### 2.3 Unprogrammed Examination (Walk-in/Emergency)
- **Route**: `/doctor/examination/new`
- **Description**: Create examination without a prior appointment

**Use Cases**:
- Emergency cases
- Walk-in patients
- Exceptions to scheduled appointments

**Features**:
- Patient search/selection (autocomplete)
- Examination date (defaults to today, cannot be future)
- Diagnosis field
- Notes field
- Automatic patient record update on save

**Flow**:
1. Doctor navigates to "New Examination"
2. Searches and selects patient
3. Fills examination details
4. Saves examination
5. Patient's examination schedule is updated

### 2.4 Schedule Appointment (Self)
- **Route**: `/doctor/appointments/new`
- **Description**: Doctor can schedule appointments for themselves

**Features**:
- Doctor field is **locked to current user** (cannot schedule for other doctors)
- Patient search/selection
- Date and Time picker with own calendar availability view
- Duration selection
- Purpose selection
- Conflict detection against own calendar

**Use Cases**:
- Patient calls doctor directly to schedule
- Doctor wants to block time for a specific patient
- Follow-up scheduling during examination

### 2.5 Doctor Dashboard Sidebar
- Today's Calendar (default view)
- Schedule Appointment (for self)
- New Examination (unprogrammed)
- My Examinations (history)
- Profile Settings

---

## 3. Nurse Features

### 3.0 Doctor Attribution (Permissions)
- **Description**: Nurses are assigned to one or more doctors by an administrator
- Nurse can only access features for their **attributed doctors**

**Permission Scope**:
- View appointments only for attributed doctors
- Schedule appointments only with attributed doctors
- View patients who have appointments with attributed doctors
- Cannot see or interact with other doctors' data

**UI Behavior**:
- Doctor dropdown only shows attributed doctors
- Calendar views filtered to attributed doctors only
- Warning message if nurse has no doctor attributions
- "My Doctors" section in profile showing attributions

**No Attribution State**:
- If nurse has no attributed doctors:
  - Cannot access appointment scheduling
  - Shows message: "Contact administrator to be assigned to doctors"
  - Can still add/edit patients (patient data is shared)

### 3.1 Patient List
- **Route**: `/nurse/patients`
- **Description**: View and manage all patients with focus on examination scheduling

**Features**:
- Paginated patient table with columns:
  - Patient Name
  - Identification Number
  - Age
  - Phone
  - Last Examination Date
  - Next Examination Date
  - Status (Active/Inactive)
  - Overdue indicator
- Search by name or identification number
- Filters:
  - Active/Inactive patients
  - Overdue for examination (highlighted)
  - Examination frequency
- Sorting by any column
- Quick actions per row:
  - View details
  - Schedule appointment
  - Edit patient

### 3.2 Patients Needing Examination
- **Route**: `/nurse/patients?filter=overdue`
- **Description**: Filtered view showing patients overdue for examination

**Highlighting Rules**:
- **Red/Urgent**: NextExaminationDate is in the past
- **Yellow/Warning**: NextExaminationDate is within 7 days
- **Green/OK**: NextExaminationDate is more than 7 days away

**Quick Actions**:
- One-click "Schedule Appointment" button
- Bulk selection for batch scheduling

### 3.3 Schedule Appointment
- **Route**: `/nurse/appointments/new?patientId={id}`
- **Description**: Create appointment for a patient with an attributed doctor
- **Permission**: Requires at least one doctor attribution

**Form Fields**:
- Patient (pre-selected or searchable)
- Doctor (dropdown of **attributed doctors only**)
- Date and Time picker
- Duration (15min, 30min, 45min, 1hr, 1.5hr, 2hr)
- Purpose (Examination, FollowUp, Consultation, Checkup, Emergency)
- Notes (optional)

**Validation**:
- Appointment must be in the future
- Duration between 15 minutes and 2 hours
- **Conflict detection**: Shows warning if doctor has overlapping appointment
- Patient and Doctor must be active
- **Doctor must be in nurse's attribution list**

**Doctor Availability**:
- Show doctor's calendar for selected date (only for attributed doctors)
- Highlight available time slots
- Visual indication of conflicts

**Access Control**:
- If nurse has no doctor attributions, show disabled state with message
- Doctor dropdown filtered to attributed doctors only

### 3.4 Add New Patient
- **Route**: `/nurse/patients/new`
- **Description**: Register a new patient in the system

**Form Fields**:
- **Personal Information**:
  - First Name (required)
  - Last Name (required)
  - Identification Number (CNP/IDNP, required, validated)
  - Identification Type (CNP/IDNP)
  - Date of Birth (required)
  - Gender (required)
- **Contact Information**:
  - Email (optional, validated format)
  - Phone (optional, E.164 format)
  - Address (optional):
    - Street
    - City
    - County
    - Postal Code
    - Country
- **Examination Schedule**:
  - Examination Frequency (Monthly, Quarterly, BiAnnually, Annually)

**Validation**:
- Identification number format and uniqueness
- Date of birth must be in the past
- Email format if provided
- Phone format (E.164) if provided

### 3.5 Edit Patient
- **Route**: `/nurse/patients/{id}/edit`
- **Description**: Update existing patient information

**Features**:
- All fields from Add Patient (except Identification Number - readonly)
- View patient history
- Deactivate/Reactivate patient

### 3.6 View Appointments (Attributed Doctors)
- **Route**: `/nurse/appointments`
- **Description**: View and manage appointments for attributed doctors only

**Features**:
- Tabs or dropdown to switch between attributed doctors
- Calendar or list view of appointments
- Filter by date range, status, purpose
- Quick actions: View details, Cancel appointment

**Permission Enforcement**:
- Only shows appointments for attributed doctors
- Cannot see appointments for non-attributed doctors
- Empty state if no doctor attributions

### 3.7 Nurse Dashboard Sidebar
- Patients (default view)
- Overdue Examinations (filtered patient list)
- New Patient
- Appointments (for attributed doctors)
- My Doctors (view attributions)
- Profile Settings

---

## 4. Shared Components

### 4.1 Navigation Header
- Application logo
- User name and role display
- Logout button

### 4.2 Patient Search Component
- Autocomplete search by name or ID
- Shows patient photo (if available), name, and ID
- Recent patients quick access

### 4.3 Doctor Selector Component
- Dropdown with active doctors
- Shows specialization
- Filter by specialization

### 4.4 Date/Time Picker
- Calendar date selection
- Time slot selection in 15-min increments
- Working hours constraint (e.g., 8:00 AM - 6:00 PM)

### 4.5 Confirmation Dialogs
- Cancel appointment confirmation
- Patient deactivation confirmation
- Unsaved changes warning

### 4.6 Toast Notifications
- Success messages (green)
- Error messages (red)
- Warning messages (yellow)
- Info messages (blue)

---

## 5. Page Layouts

### 5.1 Doctor Layout
```
┌─────────────────────────────────────────────────────────┐
│  Header: Logo | "Dr. {Name}" | Logout                   │
├────────────┬────────────────────────────────────────────┤
│            │                                            │
│  Sidebar   │           Main Content Area               │
│            │                                            │
│  Calendar  │   ┌────────────────────────────────┐      │
│  New Exam  │   │  Today's Appointments          │      │
│  History   │   │  ─────────────────────────     │      │
│  Settings  │   │  09:00 - Patient A (Checkup)   │      │
│            │   │  10:00 - Patient B (Exam)      │      │
│            │   │  11:30 - Patient C (FollowUp)  │      │
│            │   └────────────────────────────────┘      │
│            │                                            │
└────────────┴────────────────────────────────────────────┘
```

### 5.2 Nurse Layout
```
┌─────────────────────────────────────────────────────────┐
│  Header: Logo | "Nurse {Name}" | Logout                 │
├────────────┬────────────────────────────────────────────┤
│            │                                            │
│  Sidebar   │           Main Content Area               │
│            │                                            │
│  Patients  │   ┌────────────────────────────────┐      │
│  Overdue   │   │  Patient List          [+New]  │      │
│  New Pat.  │   │  Search: [___________] Filter  │      │
│  Appts     │   │  ─────────────────────────     │      │
│  Settings  │   │  Name | ID | Next Exam | Act.  │      │
│            │   │  ───────────────────────────── │      │
│            │   │  John D | 123... | ⚠️ Overdue │ 📅   │      │
│            │   │  Jane S | 456... | In 5 days  │ 📅   │      │
│            │   └────────────────────────────────┘      │
│            │                                            │
└────────────┴────────────────────────────────────────────┘
```

---

## 6. API Integration

### 6.1 Doctor Endpoints Used
| Feature | Method | Endpoint |
|---------|--------|----------|
| Get own calendar | GET | `/api/v1/appointments/doctor/{doctorId}/calendar?fromDate&toDate` |
| Confirm appointment | POST | `/api/v1/appointments/{id}/confirm` |
| Start appointment | POST | `/api/v1/appointments/{id}/start` |
| Complete appointment | POST | `/api/v1/appointments/{id}/complete` |
| Cancel appointment | POST | `/api/v1/appointments/{id}/cancel` |
| Schedule own appointment | POST | `/api/v1/appointments` |
| Create examination | POST | `/api/v1/examinations` |
| Get patient | GET | `/api/v1/patients/{id}` |
| Search patients | GET | `/api/v1/patients?search={term}` |

### 6.2 Nurse Endpoints Used
| Feature | Method | Endpoint |
|---------|--------|----------|
| List patients | GET | `/api/v1/patients?page&size&search&isOverdue` |
| Get patient | GET | `/api/v1/patients/{id}` |
| Create patient | POST | `/api/v1/patients` |
| Update patient | PUT | `/api/v1/patients/{id}` |
| Deactivate patient | POST | `/api/v1/patients/{id}/deactivate` |
| **Get attributed doctors** | GET | `/api/v1/nurses/{nurseId}/doctors` |
| Get doctor calendar | GET | `/api/v1/appointments/doctor/{doctorId}/calendar?fromDate&toDate` |
| Schedule appointment | POST | `/api/v1/appointments` |
| Get appointments | GET | `/api/v1/appointments?doctorId={attributedDoctorId}` |

### 6.3 Nurse-Doctor Attribution Endpoints (Admin)
| Feature | Method | Endpoint |
|---------|--------|----------|
| Get nurse's doctors | GET | `/api/v1/nurses/{nurseId}/doctors` |
| Assign doctor to nurse | POST | `/api/v1/nurses/{nurseId}/doctors/{doctorId}` |
| Remove doctor from nurse | DELETE | `/api/v1/nurses/{nurseId}/doctors/{doctorId}` |
| Get doctor's nurses | GET | `/api/v1/doctors/{doctorId}/nurses` |

---

## 7. State Management

### 7.1 Global State (Context)
- **AuthContext**: User info, role, token
- **ToastContext**: Notification management

### 7.2 Server State (TanStack Query)
- Patient list with pagination
- Doctor list
- Appointments
- Examinations
- Automatic cache invalidation on mutations

### 7.3 Form State (react-hook-form + zod)
- Patient form validation
- Appointment form validation
- Examination form validation

---

## 8. Route Structure

```
/login                          # Login page
/logout                         # Logout handler

# Doctor routes (protected, role: doctor)
/doctor/calendar                # Today's appointments
/doctor/calendar/:date          # Specific date calendar
/doctor/appointments/new        # Schedule appointment (self only)
/doctor/examination/new         # New unprogrammed examination
/doctor/examination/:id         # View examination details
/doctor/examinations            # Examination history
/doctor/profile                 # Profile settings

# Nurse routes (protected, role: nurse)
/nurse/patients                 # Patient list
/nurse/patients/new             # Add new patient
/nurse/patients/:id             # View patient details
/nurse/patients/:id/edit        # Edit patient
/nurse/appointments             # Appointments for attributed doctors
/nurse/appointments/new         # Schedule appointment (attributed doctors)
/nurse/doctors                  # View attributed doctors
/nurse/profile                  # Profile settings
```

---

## 9. Data Model: Nurse-Doctor Attribution

### Backend Model Required
```
NurseDoctorAttribution (Join Table)
├── NurseId (FK to Nurse)
├── DoctorId (FK to Doctor)
├── AssignedAt (DateTime)
├── AssignedBy (string - admin user)
└── IsActive (bool)
```

### Business Rules
- A nurse can be attributed to multiple doctors
- A doctor can have multiple nurses attributed
- Attribution is managed by administrators (not self-service)
- Removing attribution does not delete historical data
- Nurse sees only appointments for currently active attributions

### Permission Checks
- **Nurse scheduling appointment**: Verify DoctorId is in nurse's attribution list
- **Nurse viewing calendar**: Filter to attributed doctors only
- **Nurse viewing appointments**: Filter by attributed doctor IDs

---

## 10. Responsive Design

### Breakpoints
- **Mobile**: < 640px - Collapsed sidebar, stacked layouts
- **Tablet**: 640px - 1024px - Collapsible sidebar
- **Desktop**: > 1024px - Full sidebar, multi-column layouts

### Mobile Considerations
- Bottom navigation for primary actions
- Swipe gestures for appointment cards
- Touch-friendly button sizes (min 44px)
- Simplified patient cards in list view

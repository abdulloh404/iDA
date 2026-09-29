


library;


enum UserKind { doctor, staff }

class HospitalAccess {
  const HospitalAccess({
    required this.hospitalId,
    required this.nameTh,
    required this.nameEn,
    required this.roleCode,
    required this.roleNameTh,
    required this.roleNameEn,
    this.doctorCode,
  });

  final String hospitalId;
  final String nameTh;
  final String nameEn;
  final String roleCode;
  final String roleNameTh;
  final String roleNameEn;


  final String? doctorCode;

  String name(bool en) => en ? nameEn : nameTh;
  String roleName(bool en) => en ? roleNameEn : roleNameTh;
}

class SessionUser {
  const SessionUser({
    required this.id,
    required this.username,
    required this.displayNameTh,
    required this.displayNameEn,
    required this.positionTh,
    required this.positionEn,
    required this.email,
    required this.phone,
  });

  final String id;
  final String username;
  final String displayNameTh;
  final String displayNameEn;
  final String positionTh;
  final String positionEn;
  final String email;
  final String phone;

  String displayName(bool en) => en ? displayNameEn : displayNameTh;
  String position(bool en) => en ? positionEn : positionTh;


  String get initials {
    final name = displayNameEn.replaceAll(RegExp(r'^(Dr\.|Mr\.|Ms\.|Mrs\.)\s*'), '');
    final parts = name.split(RegExp(r'\s+')).where((e) => e.isNotEmpty).toList();
    if (parts.isEmpty) return '?';
    return parts.take(2).map((p) => p[0].toUpperCase()).join();
  }
}

class Session {
  const Session({
    required this.token,
    required this.expiresAt,
    required this.user,
    required this.hospitalId,
    required this.hospitals,
    required this.roles,
    required this.permissions,
    required this.passwordExpiresAt,
  });

  final String token;
  final DateTime expiresAt;
  final SessionUser user;
  final String hospitalId;
  final List<HospitalAccess> hospitals;
  final List<String> roles;
  final List<String> permissions;


  final DateTime passwordExpiresAt;

  HospitalAccess get hospital => hospitals.firstWhere(
        (h) => h.hospitalId == hospitalId,
        orElse: () => hospitals.first,
      );

  UserKind get kind => hospital.doctorCode != null ? UserKind.doctor : UserKind.staff;
  bool get isDoctor => kind == UserKind.doctor;
  bool can(String permission) => permissions.contains(permission);
  bool get canApprove => can('approvals.approve');

  Session withHospital(String id, {List<String>? permissions}) => Session(
        token: token,
        expiresAt: expiresAt,
        user: user,
        hospitalId: id,
        hospitals: hospitals,
        roles: roles,
        permissions: permissions ?? this.permissions,
        passwordExpiresAt: passwordExpiresAt,
      );
}


class OtpChallenge {
  const OtpChallenge({
    required this.id,
    required this.maskedPhone,
    required this.refCode,
    required this.resendAfter,
  });

  final String id;
  final String maskedPhone;
  final String refCode;
  final Duration resendAfter;
}

class SignInResult {
  const SignInResult({
    required this.session,
    required this.mustAcceptTerms,
    required this.mustChangePassword,
    required this.needsPin,
  });

  final Session session;
  final bool mustAcceptTerms;
  final bool mustChangePassword;
  final bool needsPin;
}

class PinCheck {
  const PinCheck({required this.ok, required this.attemptsLeft, this.temporary = false});

  final bool ok;
  final int attemptsLeft;


  final bool temporary;
}

class TermsSection {
  const TermsSection(this.title, this.body);
  final String title;
  final String body;
}

class TermsDocument {
  const TermsDocument({required this.version, required this.updatedAt, required this.sections});
  final String version;
  final DateTime updatedAt;
  final List<TermsSection> sections;
}


class DoctorDashboard {
  const DoctorDashboard({
    required this.todayIncome,
    required this.monthIncome,
    required this.monthLastYearDelta,
    required this.asOf,
    required this.today,
    required this.todayShifts,
  });


  final double todayIncome;
  final double monthIncome;


  final double monthLastYearDelta;
  final DateTime asOf;
  final AttendanceDay today;
  final List<DutyShift> todayShifts;
}


class GeoPoint {
  const GeoPoint(this.lat, this.lng, {this.accuracyM});
  final double lat;
  final double lng;
  final double? accuracyM;
}

class AttendanceSite {
  const AttendanceSite({
    required this.hospitalId,
    required this.nameTh,
    required this.nameEn,
    required this.location,
    required this.radiusM,
    required this.allowOutside,
  });

  final String hospitalId;
  final String nameTh;
  final String nameEn;
  final GeoPoint location;


  final double radiusM;


  final bool allowOutside;

  String name(bool en) => en ? nameEn : nameTh;
}

enum AttendanceType { checkIn, checkOut }

class AttendanceEvent {
  const AttendanceEvent({
    required this.type,
    required this.at,
    required this.distanceM,
    required this.inArea,
    required this.siteNameTh,
  });

  final AttendanceType type;
  final DateTime at;
  final double distanceM;
  final bool inArea;
  final String siteNameTh;
}

class AttendanceDay {
  const AttendanceDay({required this.date, this.checkIn, this.checkOut});

  final DateTime date;
  final AttendanceEvent? checkIn;
  final AttendanceEvent? checkOut;

  bool get isWorking => checkIn != null && checkOut == null;
  bool get isDone => checkIn != null && checkOut != null;

  Duration worked([DateTime? now]) {
    final start = checkIn?.at;
    if (start == null) return Duration.zero;
    final end = checkOut?.at ?? now ?? DateTime.now();
    final d = end.difference(start);
    return d.isNegative ? Duration.zero : d;
  }
}


enum ShiftStatus { onDuty, cancelled }

class DutyShift {
  const DutyShift({
    required this.id,
    required this.date,
    required this.startMinute,
    required this.endMinute,
    required this.clinicTh,
    required this.clinicEn,
    required this.room,
    required this.hospitalNameTh,
    required this.status,
    this.note,
  });

  final String id;
  final DateTime date;
  final int startMinute;
  final int endMinute;
  final String clinicTh;
  final String clinicEn;
  final String room;
  final String hospitalNameTh;
  final ShiftStatus status;


  final String? note;

  DateTime get start => DateTime(date.year, date.month, date.day).add(Duration(minutes: startMinute));
  DateTime get end => DateTime(date.year, date.month, date.day).add(Duration(minutes: endMinute));
  Duration get length => Duration(minutes: endMinute - startMinute);
  String clinic(bool en) => en ? clinicEn : clinicTh;
}


class Education {
  const Education(this.degree, this.institute, this.year);
  final String degree;
  final String institute;
  final int year;
}

class DoctorCodeInfo {
  const DoctorCodeInfo(this.hospitalNameTh, this.code, this.doctorType, this.active);
  final String hospitalNameTh;
  final String code;
  final String doctorType;
  final bool active;
}

class DoctorDocument {
  const DoctorDocument({
    required this.nameTh,
    required this.fileName,
    required this.uploadedAt,
    required this.sizeKb,
    this.expiresAt,
  });

  final String nameTh;
  final String fileName;
  final DateTime uploadedAt;
  final int sizeKb;
  final DateTime? expiresAt;
}

class DoctorProfile {
  const DoctorProfile({
    required this.prefixTh,
    required this.prefixEn,
    required this.firstNameTh,
    required this.lastNameTh,
    required this.firstNameEn,
    required this.lastNameEn,
    required this.nationalId,
    required this.birthDate,
    required this.passportNo,
    required this.passportExpiry,
    required this.phone,
    required this.email,
    required this.address,
    required this.licenseNo,
    required this.licenseExpiry,
    required this.specialty,
    required this.subSpecialty,
    required this.department,
    required this.education,
    required this.codes,
    required this.documents,
  });

  final String prefixTh;
  final String prefixEn;
  final String firstNameTh;
  final String lastNameTh;
  final String firstNameEn;
  final String lastNameEn;
  final String nationalId;
  final DateTime birthDate;
  final String? passportNo;
  final DateTime? passportExpiry;
  final String phone;
  final String email;
  final String address;
  final String licenseNo;
  final DateTime licenseExpiry;
  final String specialty;
  final String subSpecialty;
  final String department;
  final List<Education> education;
  final List<DoctorCodeInfo> codes;
  final List<DoctorDocument> documents;

  String get fullNameTh => '$prefixTh$firstNameTh $lastNameTh';
  String get fullNameEn => '$prefixEn $firstNameEn $lastNameEn';
}

class BankAccount {
  const BankAccount({
    required this.id,
    required this.expenseType,
    required this.bankNameTh,
    required this.bankCode,
    required this.branchNameTh,
    required this.branchCode,
    required this.accountNo,
    required this.accountName,
    required this.active,
    required this.effectiveFrom,
    required this.documentName,
  });

  final String id;


  final String expenseType;
  final String bankNameTh;
  final String bankCode;
  final String branchNameTh;
  final String branchCode;
  final String accountNo;
  final String accountName;
  final bool active;
  final DateTime effectiveFrom;
  final String documentName;
}

class BankOption {
  const BankOption(this.code, this.nameTh, this.shortName);
  final String code;
  final String nameTh;
  final String shortName;
}


class MonthlyIncome {
  const MonthlyIncome({required this.year, required this.month, required this.net, this.paidOn});
  final int year;
  final int month;
  final double net;


  final DateTime? paidOn;
  bool get paid => paidOn != null;
}

class IncomeYear {
  const IncomeYear({
    required this.year,
    required this.total,
    required this.asOf,
    required this.doctorCode,
    required this.months,
  });

  final int year;
  final double total;
  final DateTime asOf;
  final String doctorCode;
  final List<MonthlyIncome> months;
}

class DailyIncome {
  const DailyIncome({required this.date, required this.amount, required this.cases, required this.confirmed});
  final DateTime date;
  final double amount;
  final int cases;


  final bool confirmed;
}


class PaySlipRow {
  const PaySlipRow(this.label, this.values, {this.total = false});
  final String label;
  final List<double?> values;
  final bool total;
}

class PaySlipSection {
  const PaySlipSection({required this.title, required this.columns, required this.rows});
  final String title;
  final List<String> columns;
  final List<PaySlipRow> rows;
}

class PaySlip {
  const PaySlip({
    required this.year,
    required this.month,
    required this.hospitalNameTh,
    required this.doctorName,
    required this.sections,
    required this.net406,
    required this.net402,
    required this.payType,
    required this.taxId,
    required this.bankNameTh,
    required this.accountNo,
    required this.accountName,
    required this.accumulated402Income,
    required this.accumulated402Tax,
    this.paidOn,
  });

  final int year;
  final int month;
  final String hospitalNameTh;
  final String doctorName;
  final List<PaySlipSection> sections;
  final double net406;
  final double net402;
  double get total => net406 + net402;
  final String payType;
  final String taxId;
  final String bankNameTh;
  final String accountNo;
  final String accountName;
  final double accumulated402Income;
  final double accumulated402Tax;
  final DateTime? paidOn;
}


enum DocumentKind { paySlip, incomeCertificate, withholdingTax }

class DocumentRequestRecord {
  const DocumentRequestRecord({
    required this.id,
    required this.kind,
    required this.periodLabelTh,
    required this.periodLabelEn,
    required this.requestedAt,
    required this.email,
  });

  final String id;
  final DocumentKind kind;
  final String periodLabelTh;
  final String periodLabelEn;
  final DateTime requestedAt;
  final String email;
}


enum ApprovalStatus { draft, pending, approved, returned, rejected, cancelled }


abstract final class RequestType {
  static const doctorProfile = 'DOCTOR_PROFILE';
  static const doctorCode = 'DOCTOR_CODE';
  static const bankAccount = 'BANK_ACCOUNT';
  static const specialty = 'SPECIALTY';
  static const contract = 'CONTRACT';
  static const welfare = 'WELFARE';

  static const all = [doctorProfile, bankAccount, doctorCode, specialty, contract, welfare];
}

class ApprovalRequest {
  const ApprovalRequest({
    required this.id,
    required this.requestNo,
    required this.requestType,
    required this.summary,
    required this.requestedBy,
    required this.requestedAt,
    required this.updatedAt,
    required this.status,
    this.currentStepRoleTh,
  });

  final String id;
  final String requestNo;
  final String requestType;
  final String summary;
  final String requestedBy;
  final DateTime requestedAt;
  final DateTime updatedAt;
  final ApprovalStatus status;
  final String? currentStepRoleTh;

  ApprovalRequest copyWith({ApprovalStatus? status, DateTime? updatedAt, String? currentStepRoleTh}) =>
      ApprovalRequest(
        id: id,
        requestNo: requestNo,
        requestType: requestType,
        summary: summary,
        requestedBy: requestedBy,
        requestedAt: requestedAt,
        updatedAt: updatedAt ?? this.updatedAt,
        status: status ?? this.status,
        currentStepRoleTh: currentStepRoleTh,
      );
}

class FieldChange {
  const FieldChange(this.label, this.before, this.after);
  final String label;
  final String before;
  final String after;
}

class Attachment {
  const Attachment(this.name, this.sizeKb);
  final String name;
  final int sizeKb;
}

enum StepAction { approve, reject, returned }

class ApprovalStep {
  const ApprovalStep({
    required this.seq,
    required this.roleNameTh,
    this.user,
    this.action,
    this.at,
    this.comment,
  });

  final int seq;
  final String roleNameTh;
  final String? user;
  final StepAction? action;
  final DateTime? at;
  final String? comment;
}

class ApprovalDetail {
  const ApprovalDetail({
    required this.request,
    required this.doctorName,
    required this.reason,
    required this.changes,
    required this.attachments,
    required this.steps,
    required this.canDecide,
  });

  final ApprovalRequest request;
  final String doctorName;


  final String? reason;
  final List<FieldChange> changes;
  final List<Attachment> attachments;
  final List<ApprovalStep> steps;
  final bool canDecide;
}

enum Decision { approve, reject }


class EditRequestDraft {
  const EditRequestDraft({
    required this.requestType,
    required this.changes,
    required this.reason,
    required this.attachments,
  });

  final String requestType;
  final List<FieldChange> changes;
  final String reason;
  final List<Attachment> attachments;
}


enum NotificationKind { approval, income, schedule, document, security }

class AppNotification {
  const AppNotification({
    required this.id,
    required this.kind,
    required this.titleTh,
    required this.titleEn,
    required this.bodyTh,
    required this.bodyEn,
    required this.at,
    required this.read,
    this.targetId,
  });

  final String id;
  final NotificationKind kind;
  final String titleTh;
  final String titleEn;
  final String bodyTh;
  final String bodyEn;
  final DateTime at;
  final bool read;


  final String? targetId;

  AppNotification markRead() => AppNotification(
        id: id,
        kind: kind,
        titleTh: titleTh,
        titleEn: titleEn,
        bodyTh: bodyTh,
        bodyEn: bodyEn,
        at: at,
        read: true,
        targetId: targetId,
      );
}

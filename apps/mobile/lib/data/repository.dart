import '../api/client.dart';
import 'models.dart';

export '../api/client.dart' show ApiException;


abstract interface class IdaRepository {


  Future<OtpChallenge> signIn(String username, String password);
  Future<OtpChallenge> resendOtp(String challengeId);
  Future<SignInResult> verifySignInOtp(String challengeId, String code);

  Future<TermsDocument> terms();
  Future<void> acceptTerms();


  Future<void> changePassword({String? current, required String newPassword});

  Future<OtpChallenge> requestPasswordReset(String username);
  Future<String> verifyResetOtp(String challengeId, String code);
  Future<void> resetPassword(String resetToken, String newPassword);


  Future<Session?> restoreSession();
  Future<void> setPin(String pin);
  Future<PinCheck> verifyPin(String pin);


  Future<String> forgotPin();
  Future<void> signOut();
  Future<Session> switchHospital(String hospitalId);


  Future<DoctorDashboard> doctorDashboard();


  Future<DoctorProfile> doctorProfile();
  Future<List<BankAccount>> bankAccounts();
  Future<List<BankOption>> banks();


  Future<AttendanceSite> attendanceSite();
  Future<AttendanceDay> attendanceToday();
  Future<AttendanceEvent> recordAttendance(AttendanceType type, GeoPoint at);
  Future<List<AttendanceDay>> attendanceHistory(int year, int month);


  Future<List<DutyShift>> shifts(DateTime from, DateTime to);


  Future<List<int>> incomeYears();
  Future<IncomeYear> incomeYear(int year);
  Future<List<DailyIncome>> dailyIncome(int year, int month);
  Future<PaySlip> paySlip(int year, int month);


  Future<DocumentRequestRecord> requestDocument(
    DocumentKind kind, {
    required int year,
    int? month,
    int? toMonth,
  });
  Future<List<DocumentRequestRecord>> documentHistory();


  Future<List<ApprovalRequest>> myRequests();
  Future<List<ApprovalRequest>> pendingApprovals();
  Future<List<ApprovalRequest>> approvalHistory();
  Future<ApprovalDetail> approvalDetail(String id);


  Future<void> decide(List<String> ids, Decision decision, {String? comment});
  Future<ApprovalRequest> submitEditRequest(EditRequestDraft draft);


  Future<List<AppNotification>> notifications();
  Future<void> markNotificationRead(String id);
  Future<void> markAllNotificationsRead();
}

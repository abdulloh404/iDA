import 'package:flutter/widgets.dart';

import '../data/models.dart';


class S {
  const S(this.en);

  final bool en;

  static S of(BuildContext context) => S(Localizations.localeOf(context).languageCode == 'en');

  String _(String th, String english) => en ? english : th;


  String get appName => 'iDA';
  String get appTagline => _('Intelligent Doctor Application', 'Intelligent Doctor Application');
  String get ok => _('ตกลง', 'OK');
  String get cancel => _('ยกเลิก', 'Cancel');
  String get close => _('ปิด', 'Close');
  String get confirm => _('ยืนยัน', 'Confirm');
  String get save => _('บันทึก', 'Save');
  String get submit => _('ส่งคำขอ', 'Submit request');
  String get next => _('ถัดไป', 'Next');
  String get back => _('ย้อนกลับ', 'Back');
  String get retry => _('ลองอีกครั้ง', 'Try again');
  String get details => _('รายละเอียด', 'Details');
  String get seeAll => _('ดูทั้งหมด', 'See all');
  String get all => _('ทั้งหมด', 'All');
  String get today => _('วันนี้', 'Today');
  String get thisWeek => _('สัปดาห์นี้', 'This week');
  String get thisMonth => _('เดือนนี้', 'This month');
  String get loading => _('กำลังโหลด…', 'Loading…');
  String get discard => _('ทิ้งการแก้ไข', 'Discard');
  String get keepEditing => _('แก้ไขต่อ', 'Keep editing');
  String get unsavedTitle => _('ยังไม่ได้ส่งคำขอ', 'Unsent changes');
  String get unsavedBody => _('ข้อมูลที่กรอกไว้จะหายไปถ้าออกจากหน้านี้', 'What you entered will be lost if you leave.');
  String get show => _('แสดง', 'Show');
  String get hide => _('ซ่อน', 'Hide');
  String get showAmounts => _('แสดงยอดเงิน', 'Show amounts');
  String get hideAmounts => _('ซ่อนยอดเงิน', 'Hide amounts');
  String get errorTitle => _('โหลดข้อมูลไม่สำเร็จ', "Couldn't load this");
  String get errorNetwork => _('เชื่อมต่อเซิร์ฟเวอร์ไม่ได้ ตรวจสอบอินเทอร์เน็ตแล้วลองอีกครั้ง',
      "Can't reach the server. Check your connection and try again.");
  String get traceId => _('รหัสอ้างอิงปัญหา', 'Trace ID');
  String get version => _('เวอร์ชัน', 'Version');
  String get demoMode => _('โหมดสาธิต', 'Demo mode');
  String get baht => _('บาท', 'THB');
  String get required => _('จำเป็นต้องกรอก', 'Required');
  String get noData => _('ไม่พบข้อมูล', 'No data');
  String get refreshed => _('อัปเดตข้อมูลแล้ว', 'Updated');
  String get select => _('เลือก', 'Select');
  String get done => _('เสร็จ', 'Done');
  String get view => _('ดู', 'View');


  String get language => _('ภาษา', 'Language');
  String get theme => _('ธีม', 'Theme');
  String get themeSystem => _('ตามระบบ', 'System');
  String get themeLight => _('สว่าง', 'Light');
  String get themeDark => _('มืด', 'Dark');
  String get langToggleLabel => _('เปลี่ยนภาษาเป็น English', 'Switch language to Thai');


  String get welcome => _('ยินดีต้อนรับ', 'Welcome');
  String get signIn => _('เข้าสู่ระบบ', 'Sign in');
  String get signInSubtitle => _('ใช้บัญชีที่ได้รับจากโรงพยาบาลของคุณ', 'Use the account issued by your hospital');
  String get username => _('รหัสผู้ใช้งาน', 'User ID');
  String get usernameHint => _('รหัสพนักงาน เช่น D10001', 'Employee ID, e.g. D10001');
  String get password => _('รหัสผ่าน', 'Password');
  String get passwordHint => _('กรอกรหัสผ่าน', 'Enter password');
  String get rememberUsername => _('จดจำรหัสผู้ใช้งาน', 'Remember user ID');
  String get forgotPassword => _('ลืมรหัสผ่าน?', 'Forgot password?');
  String get signInHelp => _('มีปัญหาการเข้าสู่ระบบ?', 'Trouble signing in?');
  String get contactAdmin => _('ติดต่อผู้ดูแลระบบ', 'Contact admin');

  String get contactAdminBody => _('ติดต่อฝ่ายเทคโนโลยีสารสนเทศของโรงพยาบาลที่คุณสังกัด เพื่อปลดล็อกบัญชีหรือยืนยันเบอร์โทรศัพท์',
      "Contact your hospital's IT service desk to unlock your account or confirm your phone number.");
  String get demoAccounts => _('บัญชีทดสอบ', 'Demo accounts');
  String get demoDoctor => _('แพทย์', 'Doctor');
  String get demoAccounting => _('บัญชีแพทย์', 'Doctor accounting');
  String get demoMdOffice => _('สำนัก ผอ.แพทย์', 'Medical director office');
  String demoCredentials(String pw, String otp) =>
      _('รหัสผ่าน $pw · OTP $otp', 'Password $pw · OTP $otp');

  String get otpTitle => _('ยืนยันรหัส OTP', 'Verify OTP');
  String otpSentTo(String phone) =>
      _('กรอกรหัส 6 หลักที่ส่งไปยัง $phone', 'Enter the 6-digit code sent to $phone');
  String otpRef(String ref) => _('รหัสอ้างอิง: $ref', 'Ref: $ref');
  String get otpVerify => _('ยืนยันรหัส OTP', 'Verify');
  String otpResendIn(String t) => _('ส่งรหัสอีกครั้งได้ใน $t', 'Resend code in $t');
  String get otpResend => _('ส่งรหัสอีกครั้ง', 'Resend code');
  String get otpResent => _('ส่งรหัส OTP ใหม่แล้ว', 'A new code was sent');
  String get otpFieldLabel => _('รหัส OTP 6 หลัก', '6-digit code');

  String get termsTitle => _('ข้อกำหนดและเงื่อนไข', 'Terms & conditions');
  String termsVersion(String v, String date) => _('ฉบับที่ $v · ปรับปรุง $date', 'Version $v · updated $date');
  String get termsScrollDown => _('เลื่อนไปด้านล่าง', 'Scroll to bottom');
  String get termsAccept => _('ยอมรับ', 'Accept');
  String get termsDecline => _('ปฏิเสธ', 'Decline');
  String get termsReadHint => _('อ่านให้ครบก่อนกดยอมรับ', 'Read to the end to accept');
  String get termsDeclineTitle => _('ปฏิเสธข้อกำหนด?', 'Decline the terms?');
  String get termsDeclineBody =>
      _('คุณต้องยอมรับข้อกำหนดก่อนจึงจะใช้งานแอปได้ ระบบจะออกจากบัญชีนี้', "You need to accept the terms to use the app. You'll be signed out.");

  String get newPasswordTitle => _('สร้างรหัสผ่านใหม่', 'Create a new password');
  String get newPasswordSubtitle =>
      _('ตั้งรหัสผ่านของคุณเองแทนรหัสผ่านตั้งต้นที่ได้รับทาง SMS', 'Replace the temporary password from SMS with your own');
  String get currentPassword => _('รหัสผ่านปัจจุบัน', 'Current password');
  String get newPassword => _('รหัสผ่านใหม่', 'New password');
  String get confirmPassword => _('ยืนยันรหัสผ่านใหม่', 'Confirm new password');
  String get ruleLength => _('อย่างน้อย 8 ตัวอักษร', 'At least 8 characters');
  String get ruleUpper => _('ตัวพิมพ์ใหญ่ (A–Z)', 'Uppercase letter (A–Z)');
  String get ruleLower => _('ตัวพิมพ์เล็ก (a–z)', 'Lowercase letter (a–z)');
  String get ruleDigit => _('ตัวเลข (0–9)', 'Number (0–9)');
  String get ruleSpecial => _('อักขระพิเศษ เช่น ! @ # \$', 'Special character, e.g. ! @ # \$');
  String get ruleMatch => _('รหัสผ่านทั้งสองช่องตรงกัน', 'Both passwords match');
  String get passwordRulesTitle => _('รหัสผ่านต้องมี', 'Your password needs');
  String get passwordChanged => _('เปลี่ยนรหัสผ่านเรียบร้อยแล้ว', 'Password changed');

  String get setPinTitle => _('ตั้งรหัส PIN', 'Set up your PIN');
  String get setPinSubtitle => _('ใช้ PIN 6 หลักเพื่อเข้าแอปครั้งต่อไปอย่างรวดเร็ว', 'Use a 6-digit PIN to open the app quickly');
  String get confirmPinTitle => _('ยืนยันรหัส PIN', 'Confirm your PIN');
  String get confirmPinSubtitle => _('กรอกรหัส PIN อีกครั้ง', 'Enter the same PIN again');
  String get pinMismatch => _('รหัส PIN ไม่ตรงกัน กรุณาตั้งใหม่', "PINs don't match. Try again.");
  String get pinSet => _('ตั้งรหัส PIN เรียบร้อยแล้ว', 'PIN set');
  String get newPinTitle => _('ตั้งรหัส PIN ใหม่', 'Set a new PIN');
  String get tempPinNotice => _('คุณเข้าด้วย PIN ชั่วคราว กรุณาตั้ง PIN ใหม่', 'You used a temporary PIN. Set a new one.');
  String get currentPinTitle => _('กรอกรหัส PIN ปัจจุบัน', 'Enter your current PIN');

  String get unlockTitle => _('กรอกรหัส PIN', 'Enter your PIN');
  String hello(String name) => _('สวัสดี, $name', 'Hello, $name');
  String get forgotPin => _('ลืม PIN?', 'Forgot PIN?');
  String get switchAccount => _('เข้าสู่ระบบด้วยบัญชีอื่น', 'Use another account');
  String pinWrong(int left) => _('รหัส PIN ไม่ถูกต้อง เหลืออีก $left ครั้ง', 'Wrong PIN. $left attempts left.');
  String get forgotPinTitle => _('ส่ง PIN ชั่วคราวแล้ว', 'Temporary PIN sent');
  String forgotPinBody(String phone) => _('ส่งรหัส PIN ชั่วคราวไปยังเบอร์ $phone แล้ว กรุณาตรวจสอบ SMS ของคุณ',
      'We sent a temporary PIN to $phone. Check your SMS.');
  String get forgotPinConfirmTitle => _('รีเซ็ตรหัส PIN?', 'Reset your PIN?');
  String get forgotPinConfirmBody =>
      _('ระบบจะส่ง PIN ชั่วคราวทาง SMS ไปยังเบอร์ที่ลงทะเบียนไว้', "We'll text a temporary PIN to your registered number.");
  String get sendPin => _('ส่ง PIN ชั่วคราว', 'Send temporary PIN');
  String get backspace => _('ลบ', 'Delete');

  String get forgotTitle => _('ลืมรหัสผ่าน', 'Forgot password');
  String get forgotSubtitle => _('กรอกรหัสผู้ใช้งาน ระบบจะส่ง OTP ไปยังเบอร์โทรศัพท์ที่ลงทะเบียนไว้',
      "Enter your user ID and we'll text a code to your registered phone");
  String get forgotEmailUsers =>
      _('ผู้ใช้งานที่ไม่มีเบอร์โทรศัพท์ในระบบ กรุณาติดต่อผู้ดูแลระบบ', "If you don't have a phone on file, contact the admin.");
  String get sendOtp => _('ส่งรหัส OTP', 'Send code');
  String get resetDone => _('ตั้งรหัสผ่านใหม่เรียบร้อย', 'Password reset');
  String get resetDoneBody => _('เข้าสู่ระบบด้วยรหัสผ่านใหม่ได้เลย', 'Sign in with your new password.');
  String get backToSignIn => _('กลับไปหน้าเข้าสู่ระบบ', 'Back to sign in');
  String stepOf(int n, int total) => _('ขั้นที่ $n จาก $total', 'Step $n of $total');


  String get navHome => _('หน้าหลัก', 'Home');
  String get navServices => _('บริการ', 'Services');
  String get navNotifications => _('แจ้งเตือน', 'Alerts');
  String get navAccount => _('บัญชี', 'Account');
  String get selectHospital => _('เลือกโรงพยาบาล', 'Choose hospital');
  String get selectHospitalHint =>
      _('ข้อมูลในแอปจะแสดงตามโรงพยาบาลที่เลือก', 'Everything in the app follows the hospital you choose');
  String switchedTo(String name) => _('เปลี่ยนเป็น $name แล้ว', 'Switched to $name');
  String get doctorCode => _('รหัสแพทย์', 'Doctor code');
  String get sessionLocked => _('ล็อกแอปแล้วเพื่อความปลอดภัย', 'Locked for your security');


  String get goodMorning => _('สวัสดีตอนเช้า', 'Good morning');
  String get goodAfternoon => _('สวัสดีตอนบ่าย', 'Good afternoon');
  String get goodEvening => _('สวัสดีตอนเย็น', 'Good evening');
  String get todayIncome => _('รายได้วันนี้', "Today's income");
  String get monthIncome => _('รายได้เดือนนี้', 'This month');
  String get estimated => _('ยอดประมาณการ', 'Estimated');
  String asOf(String d) => _('ข้อมูล ณ $d', 'As of $d');
  String vsLastYear(String p) => _('$p เทียบเดือนเดียวกันปีก่อน', '$p vs. same month last year');
  String get todayWork => _('การปฏิบัติงานวันนี้', 'Today at work');
  String get notCheckedIn => _('ยังไม่ได้ลงเวลาเข้างาน', "You haven't checked in");
  String workingSince(String t) => _('กำลังปฏิบัติงาน ตั้งแต่ $t', 'On duty since $t');
  String checkedOutAt(String t) => _('ลงเวลาออกแล้ว $t', 'Checked out at $t');
  String get noShiftToday => _('วันนี้ไม่มีเวรออกตรวจ', 'No clinic session today');
  String get quickMenu => _('เมนูลัด', 'Shortcuts');
  String get pendingForYou => _('รอคุณพิจารณา', 'Waiting for you');
  String pendingCount(int n) => _('$n รายการ', '$n items');
  String get reviewNow => _('พิจารณาเลย', 'Review now');
  String get allCaughtUp => _('ไม่มีรายการค้าง', "You're all caught up");
  String get allCaughtUpBody => _('รายการใหม่จะแจ้งเตือนให้ทราบทันที', "We'll notify you when something arrives.");


  String get groupMyInfo => _('ข้อมูลของฉัน', 'My information');
  String get groupWork => _('การปฏิบัติงาน', 'Work');
  String get groupIncome => _('รายได้และภาษี', 'Income & tax');
  String get groupRequests => _('คำขอและการอนุมัติ', 'Requests & approvals');
  String get menuProfile => _('ประวัติแพทย์', 'Doctor profile');
  String get menuMyInfo => _('ข้อมูลของฉัน', 'My info');
  String get menuBank => _('บัญชีธนาคาร', 'Bank account');
  String get menuEditProfile => _('ขอแก้ไขประวัติ', 'Edit profile');
  String get menuEditBank => _('ขอแก้ไขบัญชี', 'Change bank');
  String get menuCheckIn => _('ลงเวลาเข้างาน', 'Check in');
  String get menuAttendanceHistory => _('ประวัติลงเวลา', 'Time records');
  String get menuSchedule => _('ตารางเวร', 'Duty schedule');
  String get menuIncome => _('สรุปรายได้', 'Income');
  String get menuDocuments => _('เอกสารรายได้', 'Tax documents');
  String get menuMyRequests => _('คำขอของฉัน', 'My requests');
  String get menuPending => _('รอดำเนินการ', 'Pending');
  String get menuHistory => _('ประวัติคำขอ', 'Request history');


  String get checkIn => _('เช็คอิน', 'Check in');
  String get checkOut => _('เช็คเอาท์', 'Check out');
  String get workHours => _('ชั่วโมงการทำงาน', 'Hours worked');
  String get statusNotStarted => _('ยังไม่ลงเวลา', 'Not checked in');
  String get statusWorking => _('กำลังปฏิบัติงาน', 'On duty');
  String get statusDone => _('ลงเวลาครบแล้ว', 'Day complete');
  String get locating => _('กำลังหาตำแหน่ง…', 'Finding your location…');
  String get refreshLocation => _('รีเฟรชตำแหน่ง', 'Refresh location');
  String distanceFrom(String d) => _('ห่างจากโรงพยาบาล $d', '$d from the hospital');
  String inArea(String r) => _('อยู่ในพื้นที่ (รัศมี $r)', 'Inside the area ($r radius)');
  String outArea(String r) => _('อยู่นอกพื้นที่ (รัศมี $r)', 'Outside the area ($r radius)');
  String get outAreaAllowed => _('อนุญาตให้ลงเวลานอกพื้นที่ รายการจะถูกทำเครื่องหมายไว้',
      'Out-of-area check-in is allowed and will be flagged');
  String get outAreaBlocked => _('ต้องอยู่ในรัศมีที่โรงพยาบาลกำหนดจึงจะลงเวลาได้',
      'Move within the hospital radius to check in');
  String accuracy(String m) => _('ความแม่นยำ ±$m', 'Accuracy ±$m');
  String get locationDenied => _('แอปยังไม่ได้รับสิทธิ์เข้าถึงตำแหน่ง', "The app can't access your location");
  String get locationDeniedBody => _('ต้องใช้ตำแหน่งเพื่อยืนยันว่าคุณอยู่ที่โรงพยาบาล', 'We need it to confirm you are at the hospital.');
  String get locationOff => _('GPS ของเครื่องปิดอยู่', 'Location services are off');
  String get allowLocation => _('อนุญาตการเข้าถึงตำแหน่ง', 'Allow location');
  String get openSettings => _('เปิดการตั้งค่า', 'Open settings');
  String confirmCheckTitle(bool isIn) => isIn ? _('ยืนยันการเช็คอิน', 'Confirm check-in') : _('ยืนยันการเช็คเอาท์', 'Confirm check-out');
  String confirmCheckBody(bool isIn, String place) => isIn
      ? _('คุณต้องการเช็คอินที่ $place ใช่หรือไม่', 'Check in at $place?')
      : _('คุณต้องการเช็คเอาท์ที่ $place ใช่หรือไม่', 'Check out at $place?');
  String checkedInToast(String t) => _('เช็คอินสำเร็จ เวลา $t', 'Checked in at $t');
  String checkedOutToast(String t) => _('เช็คเอาท์สำเร็จ เวลา $t', 'Checked out at $t');
  String get todayLog => _('บันทึกของวันนี้', "Today's log");
  String get history => _('ประวัติ', 'History');
  String get timeIn => _('เข้า', 'In');
  String get timeOut => _('ออก', 'Out');
  String get timeLabel => _('เวลา', 'Time');
  String get distanceLabel => _('ระยะห่าง', 'Distance');
  String get inAreaShort => _('ในพื้นที่', 'In area');
  String get flaggedOutside => _('นอกพื้นที่', 'Out of area');
  String get stillWorking => _('ยังไม่ลงเวลาออก', 'No check-out');
  String get attendanceEmpty => _('ยังไม่มีบันทึกเวลาในเดือนนี้', 'No time records this month');
  String get attendanceEmptyBody => _('บันทึกจะแสดงที่นี่หลังจากคุณเช็คอิน', 'Your records appear here after you check in.');
  String daysWorked(int n) => _('$n วัน', '$n days');
  String get totalHours => _('รวมชั่วโมง', 'Total hours');
  String get daysLabel => _('วันทำงาน', 'Days');


  String get onDuty => _('ออกตรวจ', 'Clinic');
  String get cancelledDuty => _('งดตรวจ', 'Cancelled');
  String get selectedDay => _('วันที่เลือก', 'Selected day');
  String get noShiftsDay => _('ไม่มีเวรในวันนี้', 'No sessions on this day');
  String get noShiftsDayBody => _('เลือกวันที่มีจุดบนปฏิทินเพื่อดูเวร', 'Pick a day with a marker to see sessions.');
  String get noShiftsRange => _('ไม่มีเวรในช่วงนี้', 'No sessions in this period');
  String shiftCount(int n) => _('$n เวร', '$n sessions');
  String get prevMonth => _('เดือนก่อนหน้า', 'Previous month');
  String get nextMonth => _('เดือนถัดไป', 'Next month');
  String get legend => _('สัญลักษณ์', 'Legend');


  String get incomeLockTitle => _('ยืนยันตัวตนเพื่อดูรายได้', 'Verify to view income');
  String get incomeLockBody => _('ข้อมูลรายได้เป็นข้อมูลส่วนบุคคล กรุณากรอกรหัส PIN', 'Income is personal data. Enter your PIN.');
  String yearTotal(String y) => _('รายได้สุทธิสะสมปี $y', 'Net income for $y');
  String get monthly => _('รายเดือน', 'Monthly');
  String get daily => _('รายวัน', 'Daily');
  String get paid => _('จ่ายแล้ว', 'Paid');
  String get awaitingPay => _('รอจ่าย', 'Pending');
  String paidOn(String d) => _('จ่าย $d', 'Paid $d');
  String cases(int n) => _('$n รายการ', '$n cases');
  String get unconfirmed => _('ยังไม่ปิดยอด', 'Not final');
  String get chartMonthly => _('รายได้สุทธิรายเดือน', 'Net income by month');
  String get chartHint => _('แตะแท่งกราฟเพื่อดูใบแจ้งรายได้', 'Tap a bar to open the pay slip');
  String get dailyNote => _('ยอดรายวันเป็นส่วนแบ่งค่าแพทย์ 40(6) ก่อนหักภาษี ยอดของวันนี้เป็นยอดประมาณการจนกว่าจะปิดวัน',
      "Daily figures are your 40(6) share before tax. Today's figure is an estimate until the day closes.");
  String get paySlip => _('ใบแจ้งรายได้', 'Pay slip');
  String paySlipFor(String m) => _('ใบแจ้งรายได้ ประจำเดือน $m', 'Pay slip · $m');
  String get netTotal => _('รวมเงินจ่ายแพทย์ทั้งสิ้น', 'Total paid');
  String get paymentInfo => _('ข้อมูลการจ่าย', 'Payment details');
  String get taxId => _('เลขประจำตัวผู้เสียภาษี', 'Tax ID');
  String get payType => _('ประเภทการจ่าย', 'Payment method');
  String get accountNo => _('เลขที่บัญชี', 'Account no.');
  String get bank => _('ธนาคาร', 'Bank');
  String get accountName => _('ชื่อบัญชี', 'Account name');
  String get accumulated402 => _('ภาษี 40(2) สะสม', '40(2) year to date');
  String get accumulatedIncome => _('รายได้ 40(2) สะสม', '40(2) income YTD');
  String get accumulatedTax => _('ภาษีหัก ณ ที่จ่าย สะสม', 'Tax withheld YTD');
  String get sendToEmail => _('ส่งเอกสารเข้าอีเมล', 'Email this document');
  String get confidential => _('เอกสารภายใน ห้ามเผยแพร่', 'Internal · do not share');
  String get summary406 => _('รวม 40(6) สุทธิ', '40(6) net');
  String get summary402 => _('รวม 40(2) สุทธิ', '40(2) net');


  String get documentsTitle => _('เอกสารรายได้และภาษี', 'Income & tax documents');
  String get documentsIntro => _('เอกสารจะส่งเป็นไฟล์ PDF เข้ารหัสไปยังอีเมลที่ลงทะเบียนไว้',
      'Documents are sent as password-protected PDFs to your registered email');
  String docKind(DocumentKind k) => switch (k) {
        DocumentKind.paySlip => _('ใบแจ้งรายได้ (Pay Slip)', 'Pay slip'),
        DocumentKind.incomeCertificate => _('หนังสือรับรองรายได้แพทย์ มาตรา 40(6)', 'Income certificate · 40(6)'),
        DocumentKind.withholdingTax => _('หนังสือรับรองการหักภาษี ณ ที่จ่าย (50 ทวิ)', 'Withholding tax certificate (50 ทวิ)'),
      };
  String docKindHint(DocumentKind k) => switch (k) {
        DocumentKind.paySlip => _('สรุปรายได้และรายการหักรายเดือน', 'Monthly income and deductions'),
        DocumentKind.incomeCertificate => _('ใช้ประกอบการยื่นภาษีหรือยื่นกู้ เลือกช่วงเดือนได้', 'For tax filing or loans — choose a month range'),
        DocumentKind.withholdingTax => _('หนังสือรับรองภาษีหัก ณ ที่จ่ายประจำปีภาษี', 'Annual certificate for your tax return'),
      };
  String get period => _('งวด', 'Period');
  String get fromMonth => _('ตั้งแต่เดือน', 'From');
  String get toMonth => _('ถึงเดือน', 'To');
  String get taxYear => _('ปีภาษี', 'Tax year');
  String get month => _('เดือน', 'Month');
  String get year => _('ปี', 'Year');
  String get sendTo => _('ส่งไปที่', 'Send to');
  String get pdfPassword => _('รหัสเปิดไฟล์', 'File password');
  String get pdfPasswordHint => _('วันเดือนปีเกิด ค.ศ. แบบ DDMMYYYY เช่น เกิด 7 ธันวาคม 1981 = 07121981',
      'Your birth date as DDMMYYYY, e.g. 7 Dec 1981 = 07121981');
  String get invalidRange => _('เดือนสิ้นสุดต้องไม่ก่อนเดือนเริ่มต้น', 'End month must not be before the start month');
  String get sendDocument => _('ส่งเอกสาร', 'Send document');
  String docSent(String email) => _('ส่งเอกสารไปที่ $email แล้ว', 'Sent to $email');
  String get recentRequests => _('เอกสารที่ขอล่าสุด', 'Recently requested');
  String get sent => _('ส่งแล้ว', 'Sent');
  String get noDocuments => _('ยังไม่เคยขอเอกสาร', 'No documents requested yet');


  String get tabPersonal => _('ประวัติส่วนตัว', 'Personal');
  String get tabOther => _('ประวัติอื่น ๆ', 'Other');
  String get tabDocuments => _('เอกสาร', 'Documents');
  String get personalInfo => _('ข้อมูลส่วนตัว', 'Personal information');
  String get contactInfo => _('ข้อมูลติดต่อ', 'Contact');
  String get professional => _('วิชาชีพ', 'Professional');
  String get educationTitle => _('การศึกษา', 'Education');
  String get doctorCodes => _('รหัสแพทย์ตามโรงพยาบาล', 'Doctor codes by hospital');
  String get prefixTh => _('คำนำหน้า (TH)', 'Title (TH)');
  String get prefixEn => _('คำนำหน้า (EN)', 'Title (EN)');
  String get nameTh => _('ชื่อ - นามสกุล (TH)', 'Name (TH)');
  String get nameEn => _('ชื่อ - นามสกุล (EN)', 'Name (EN)');
  String get nationalId => _('เลขที่บัตรประชาชน', 'National ID');
  String get birthDate => _('วันเกิด', 'Date of birth');
  String get passportNo => _('เลขที่หนังสือเดินทาง', 'Passport no.');
  String get passportExpiry => _('วันหมดอายุหนังสือเดินทาง', 'Passport expiry');
  String get phone => _('เบอร์โทรศัพท์', 'Phone');
  String get email => _('อีเมล', 'Email');
  String get address => _('ที่อยู่', 'Address');
  String get licenseNo => _('เลขที่ใบประกอบวิชาชีพ', 'License no.');
  String get licenseExpiry => _('วันหมดอายุใบประกอบวิชาชีพ', 'License expiry');
  String get specialty => _('ความเชี่ยวชาญ', 'Specialty');
  String get subSpecialty => _('อนุสาขา', 'Subspecialty');
  String get department => _('แผนก', 'Department');
  String get active => _('ใช้งาน', 'Active');
  String get inactive => _('ไม่ใช้งาน', 'Inactive');
  String get requestEdit => _('ขอแก้ไขข้อมูล', 'Request a change');
  String expiresOn(String d) => _('หมดอายุ $d', 'Expires $d');
  String uploadedOn(String d) => _('อัปโหลด $d', 'Uploaded $d');
  String get maskedHint => _('แตะเพื่อแสดงข้อมูลเต็ม', 'Tap to reveal');
  String get bankTitle => _('บัญชีธนาคาร', 'Bank account');
  String get bankForPayment => _('บัญชีรับเงินค่าแพทย์', 'Account for doctor fee payments');
  String get expenseType => _('ประเภทค่าใช้จ่าย', 'Expense type');
  String get branch => _('สาขาธนาคาร', 'Branch');
  String get accountHolder => _('ชื่อบัญชีธนาคาร หรือ ผู้รับเช็ค', 'Account name or payee');
  String get effectiveFrom => _('มีผลตั้งแต่', 'Effective from');
  String get viewDocument => _('ดูไฟล์เอกสาร', 'View document');
  String get previousAccounts => _('บัญชีที่เคยใช้', 'Previous accounts');
  String get documentPreviewNote =>
      _('ไฟล์เอกสารเปิดได้เมื่อเชื่อมต่อระบบจริง', 'Files open once connected to the live system');


  String get editProfileTitle => _('ขอแก้ไขประวัติแพทย์', 'Request profile change');
  String get editBankTitle => _('ขอแก้ไขบัญชีธนาคาร', 'Request bank change');
  String get editIntro => _('เลือกข้อมูลที่ต้องการแก้ไข ข้อมูลจริงจะเปลี่ยนหลังคำขอได้รับอนุมัติ',
      'Pick what to change. Your data updates only after approval.');
  String get currentValue => _('ปัจจุบัน', 'Current');
  String get newValue => _('ค่าใหม่', 'New value');
  String get reason => _('เหตุผลการขอแก้ไข', 'Reason');
  String get reasonHint => _('เช่น ย้ายที่อยู่ เปลี่ยนเบอร์ติดต่อ', 'e.g. moved house, new phone number');
  String get attachments => _('เอกสารแนบ', 'Attachments');
  String get attachHint => _('PDF, JPG หรือ PNG ไม่เกิน 5 MB', 'PDF, JPG or PNG up to 5 MB');
  String get addAttachment => _('แนบไฟล์', 'Attach file');
  String get bankBookRequired => _('ต้องแนบสำเนาหน้าสมุดบัญชี', 'Attach a copy of your bank book');
  String get approvalRoute => _('ขั้นตอนการอนุมัติ', 'Approval route');
  String get routeYou => _('คุณส่งคำขอ', 'You submit');
  String routeNotify(String cc) => _('แจ้งผลทางอีเมล (สำเนา $cc)', 'Result by email (cc $cc)');
  String get pickAtLeastOne => _('เลือกข้อมูลที่ต้องการแก้ไขอย่างน้อย 1 รายการ', 'Choose at least one field to change');
  String get sameAsCurrent => _('ค่าใหม่ต้องไม่เหมือนค่าปัจจุบัน', 'New value must differ from the current one');
  String get invalidEmail => _('รูปแบบอีเมลไม่ถูกต้อง', 'Enter a valid email');
  String get invalidPhone => _('เบอร์โทรศัพท์ต้องเป็นตัวเลข 10 หลัก', 'Phone must be 10 digits');
  String get invalidAccount => _('เลขที่บัญชีต้องเป็นตัวเลข 10–12 หลัก', 'Account number must be 10–12 digits');
  String get requestSent => _('ส่งคำขอเรียบร้อย', 'Request sent');
  String requestSentBody(String no, String role) =>
      _('คำขอเลขที่ $no ถูกส่งไปยัง$role แล้ว คุณจะได้รับแจ้งผลทางแอปและอีเมล', 'Request $no went to $role. We\'ll notify you in the app and by email.');
  String get viewRequest => _('ดูคำขอ', 'View request');
  String get backHome => _('กลับหน้าหลัก', 'Back to home');
  String get newBank => _('ธนาคารใหม่', 'New bank');
  String get chooseBank => _('เลือกธนาคาร', 'Choose a bank');
  String get branchHint => _('เช่น สาขาพญาไท', 'e.g. Phaya Thai branch');
  String get accountNoHint => _('ตัวเลขเท่านั้น', 'Digits only');
  String get removeFile => _('ลบไฟล์', 'Remove file');


  String get myRequestsTitle => _('คำขอของฉัน', 'My requests');
  String get pendingTitle => _('รายการรอดำเนินการ', 'Pending approvals');
  String get historyTitle => _('ประวัติคำขอ', 'Request history');
  String requestType(String code) => switch (code) {
        RequestType.doctorProfile => _('ข้อมูลประวัติแพทย์', 'Doctor profile'),
        RequestType.doctorCode => _('ข้อมูลรหัสแพทย์', 'Doctor code'),
        RequestType.bankAccount => _('บัญชีธนาคารของแพทย์', 'Bank account'),
        RequestType.specialty => _('ความเชี่ยวชาญของแพทย์', 'Specialty'),
        RequestType.contract => _('สัญญาแพทย์', 'Contract'),
        RequestType.welfare => _('สวัสดิการแพทย์', 'Welfare'),
        _ => code,
      };
  String status(ApprovalStatus s) => switch (s) {
        ApprovalStatus.draft => _('ร่าง', 'Draft'),
        ApprovalStatus.pending => _('รออนุมัติ', 'Pending'),
        ApprovalStatus.approved => _('อนุมัติแล้ว', 'Approved'),
        ApprovalStatus.returned => _('ส่งกลับแก้ไข', 'Returned'),
        ApprovalStatus.rejected => _('ไม่อนุมัติ', 'Rejected'),
        ApprovalStatus.cancelled => _('ยกเลิก', 'Cancelled'),
      };
  String get requestNo => _('เลขที่รายการ', 'Request no.');
  String get requester => _('ผู้ขอ', 'Requested by');
  String get submittedAt => _('วันที่ส่ง', 'Submitted');
  String waitingAt(String role) => _('รอ$role', 'Waiting for $role');
  String get approve => _('อนุมัติ', 'Approve');
  String get reject => _('ไม่อนุมัติ', 'Reject');
  String get rejectTitle => _('ยืนยันการไม่อนุมัติ', 'Reject request');
  String rejectManyTitle(int n) => _('ไม่อนุมัติ $n รายการ', 'Reject $n requests');
  String get rejectReason => _('เหตุผลที่ไม่อนุมัติ', 'Reason for rejecting');
  String get rejectReasonHint => _('ผู้ขอจะเห็นเหตุผลนี้ เขียนให้รู้ว่าต้องแก้อะไร', 'The requester sees this — say what to fix');
  String get rejectReasonRequired => _('กรุณาระบุเหตุผล', 'Please give a reason');
  String get approveTitle => _('ยืนยันการอนุมัติ', 'Approve request');
  String approveManyTitle(int n) => _('อนุมัติ $n รายการ', 'Approve $n requests');
  String get approveBody => _('ข้อมูลจริงจะถูกปรับตามคำขอทันทีหลังอนุมัติ', 'The data changes as soon as you approve.');
  String approvedToast(int n) => _('อนุมัติแล้ว $n รายการ', 'Approved $n');
  String rejectedToast(int n) => _('ไม่อนุมัติ $n รายการ', 'Rejected $n');
  String selectedCount(int n) => _('เลือกแล้ว $n รายการ', '$n selected');
  String get selectAll => _('เลือกทั้งหมด', 'Select all');
  String get changes => _('ข้อมูลที่ขอแก้ไข', 'Requested changes');
  String get before => _('เดิม', 'Before');
  String get after => _('ใหม่', 'After');
  String get timeline => _('ขั้นตอนการอนุมัติ', 'Approval steps');
  String get submitted => _('ส่งคำขอ', 'Submitted');
  String get waiting => _('รอพิจารณา', 'Waiting');
  String get noteFromRequester => _('เหตุผลของผู้ขอ', "Requester's reason");
  String get noteFromApprover => _('ความเห็นผู้พิจารณา', "Reviewer's comment");
  String get emptyPending => _('ไม่มีรายการรอดำเนินการ', 'Nothing waiting for you');
  String get emptyPendingBody => _('คำขอใหม่ที่ต้องพิจารณาจะแสดงที่นี่', 'New requests to review will appear here.');
  String get emptyMine => _('ยังไม่มีคำขอ', 'No requests yet');
  String get emptyMineBody => _('ขอแก้ไขประวัติหรือบัญชีธนาคารได้จากเมนูบริการ', 'Request profile or bank changes from Services.');
  String get emptyHistory => _('ยังไม่มีประวัติคำขอ', 'No history yet');
  String get emptyHistoryBody => _('คำขอที่พิจารณาแล้วจะแสดงที่นี่', 'Decided requests will appear here.');
  String get noMatch => _('ไม่พบรายการที่ตรงกับตัวกรอง', 'Nothing matches this filter');
  String get clearFilter => _('ล้างตัวกรอง', 'Clear filter');
  String get searchRequests => _('ค้นหาเลขที่ ชื่อผู้ขอ หรือรายละเอียด', 'Search number, requester or detail');
  String get filterType => _('ประเภท', 'Type');
  String get filterStatus => _('สถานะ', 'Status');
  String get quickApprove => _('อนุมัติรายการนี้', 'Approve this request');
  String get quickReject => _('ไม่อนุมัติรายการนี้', 'Reject this request');


  String get notificationsTitle => _('การแจ้งเตือน', 'Notifications');
  String get markAllRead => _('อ่านทั้งหมด', 'Mark all read');
  String get unread => _('ยังไม่อ่าน', 'Unread');
  String get yesterday => _('เมื่อวาน', 'Yesterday');
  String get earlier => _('ก่อนหน้านี้', 'Earlier');
  String get emptyNotifications => _('ยังไม่มีการแจ้งเตือน', 'No notifications');
  String get emptyNotificationsBody => _('ผลการอนุมัติ ใบแจ้งรายได้ และตารางเวรใหม่จะแจ้งที่นี่',
      'Approvals, pay slips and schedule updates show up here.');
  String get newBadge => _('ใหม่', 'New');


  String get accountTitle => _('บัญชีของฉัน', 'My account');
  String get security => _('ความปลอดภัย', 'Security');
  String get changePassword => _('เปลี่ยนรหัสผ่าน', 'Change password');
  String passwordExpiresIn(int d) => _('หมดอายุในอีก $d วัน', 'Expires in $d days');
  String get changePin => _('เปลี่ยนรหัส PIN', 'Change PIN');
  String get display => _('การแสดงผล', 'Display');
  String get about => _('เกี่ยวกับ', 'About');
  String get signOut => _('ออกจากระบบ', 'Sign out');
  String get signOutTitle => _('ออกจากระบบ?', 'Sign out?');
  String get signOutBody => _('ครั้งหน้าต้องเข้าสู่ระบบด้วยรหัสผ่านและ OTP แล้วตั้ง PIN ใหม่',
      "Next time you'll need your password, an OTP and a new PIN.");
  String get pinChanged => _('เปลี่ยนรหัส PIN เรียบร้อยแล้ว', 'PIN changed');
  String get privacy => _('การคุ้มครองข้อมูลส่วนบุคคล (PDPA)', 'Privacy (PDPA)');
  String get hospitalLabel => _('โรงพยาบาล', 'Hospital');
  String get roleLabel => _('บทบาท', 'Role');
  String get passwordExpiringTitle => _('รหัสผ่านใกล้หมดอายุ', 'Password expiring soon');
  String passwordExpiringBody(int d) =>
      _('รหัสผ่านจะหมดอายุในอีก $d วัน เปลี่ยนตอนนี้เพื่อไม่ให้ถูกล็อก', 'Your password expires in $d days. Change it now to avoid a lockout.');
}

import 'dart:math' as math;

import 'package:shared_preferences/shared_preferences.dart';

import '../core/format.dart';
import 'models.dart';
import 'repository.dart';


class DemoRepository implements IdaRepository {
  DemoRepository(this._prefs, {this.latency = const Duration(milliseconds: 350), DateTime Function()? clock})
      : _clock = clock ?? DateTime.now {
    _seed();
  }

  static const demoPassword = 'Ida@2026';
  static const demoOtp = '123456';
  static const demoTempPin = '135790';
  static const maxPinAttempts = 5;

  final SharedPreferences _prefs;
  final Duration latency;
  final DateTime Function() _clock;

  DateTime get _now => _clock();


  _Account? _pending;
  _Account? _account;
  String? _hospitalId;
  final _challenges = <String, _Account>{};
  final _resetTokens = <String, _Account>{};
  final _attendance = <String, AttendanceDay>{};
  final _requests = <ApprovalRequest>[];
  final _details = <String, ApprovalDetail>{};
  final _notifications = <String, List<AppNotification>>{};
  final _documents = <DocumentRequestRecord>[];
  var _seq = 40;

  Future<void> _delay() => latency == Duration.zero ? Future.value() : Future.delayed(latency);

  Never _fail(int status, String code, String message) =>
      throw ApiException(status, code, message, traceId: 'demo-${_now.millisecondsSinceEpoch.toRadixString(16)}');

  _Account get _me => _account ?? _fail(401, 'unauthorized', 'เซสชันหมดอายุ กรุณาเข้าสู่ระบบอีกครั้ง');


  @override
  Future<OtpChallenge> signIn(String username, String password) async {
    await _delay();
    final acc = _accounts[username.trim().toUpperCase()];
    if (acc == null || !_passwordOk(acc, password)) {
      _fail(401, 'invalid_credentials', 'รหัสผู้ใช้งานหรือรหัสผ่านไม่ถูกต้อง');
    }
    _pending = acc;
    return _newChallenge(acc);
  }

  OtpChallenge _newChallenge(_Account acc) {
    final id = 'otp-${_now.microsecondsSinceEpoch}';
    _challenges[id] = acc;
    final ref = String.fromCharCodes(
      List.generate(4, (i) => 65 + (_now.microsecond + i * 7) % 26),
    );
    return OtpChallenge(
      id: id,
      maskedPhone: Fmt.maskPhone(acc.user.phone),
      refCode: ref,
      resendAfter: const Duration(seconds: 60),
    );
  }

  @override
  Future<OtpChallenge> resendOtp(String challengeId) async {
    await _delay();
    final acc = _challenges.remove(challengeId) ?? _fail(410, 'otp_expired', 'รหัส OTP หมดอายุ กรุณาเริ่มใหม่');
    return _newChallenge(acc);
  }

  void _checkOtp(String challengeId, String code) {
    if (!_challenges.containsKey(challengeId)) {
      _fail(410, 'otp_expired', 'รหัส OTP หมดอายุ กรุณาขอรหัสใหม่');
    }
    if (code != demoOtp) _fail(400, 'otp_invalid', 'รหัส OTP ไม่ถูกต้อง กรุณาตรวจสอบ SMS อีกครั้ง');
  }

  @override
  Future<SignInResult> verifySignInOtp(String challengeId, String code) async {
    await _delay();
    _checkOtp(challengeId, code);
    final acc = _challenges.remove(challengeId)!;
    if (_pending?.user.username != acc.user.username) _fail(409, 'otp_mismatch', 'กรุณาเข้าสู่ระบบใหม่');
    _account = acc;
    _hospitalId = acc.hospitals.first.hospitalId;
    final activated = _prefs.getBool('demo.activated.${acc.user.username}') ?? false;
    final pinOwner = _prefs.getString('demo.pin.user');
    return SignInResult(
      session: _session(),
      mustAcceptTerms: !activated,
      mustChangePassword: !activated,
      needsPin: pinOwner != acc.user.username,
    );
  }

  @override
  Future<TermsDocument> terms() async {
    await _delay();
    return TermsDocument(version: '2.1', updatedAt: DateTime(2026, 6, 15), sections: _termsSections);
  }

  @override
  Future<void> acceptTerms() async {
    await _delay();
    _prefs.setString('demo.terms.${_me.user.username}', '2.1');
  }

  @override
  Future<void> changePassword({String? current, required String newPassword}) async {
    await _delay();
    final acc = _me;
    if (current != null && !_passwordOk(acc, current)) {
      _fail(400, 'current_password_invalid', 'รหัสผ่านปัจจุบันไม่ถูกต้อง');
    }
    if (newPassword == _passwordOf(acc)) {
      _fail(400, 'password_reused', 'รหัสผ่านใหม่ต้องไม่ซ้ำกับรหัสผ่านเดิม');
    }
    await _prefs.setString('demo.password.${acc.user.username}', newPassword);
    await _prefs.setBool('demo.activated.${acc.user.username}', true);
  }

  @override
  Future<OtpChallenge> requestPasswordReset(String username) async {
    await _delay();
    final acc = _accounts[username.trim().toUpperCase()] ??
        _fail(404, 'user_not_found', 'ไม่พบรหัสผู้ใช้งานนี้ในระบบ กรุณาตรวจสอบหรือติดต่อผู้ดูแลระบบ');
    return _newChallenge(acc);
  }

  @override
  Future<String> verifyResetOtp(String challengeId, String code) async {
    await _delay();
    _checkOtp(challengeId, code);
    final acc = _challenges.remove(challengeId)!;
    final token = 'reset-${_now.microsecondsSinceEpoch}';
    _resetTokens[token] = acc;
    return token;
  }

  @override
  Future<void> resetPassword(String resetToken, String newPassword) async {
    await _delay();
    final acc = _resetTokens.remove(resetToken) ?? _fail(410, 'reset_expired', 'ลิงก์รีเซ็ตหมดอายุ กรุณาเริ่มใหม่');
    await _prefs.setString('demo.password.${acc.user.username}', newPassword);
    await _prefs.setBool('demo.activated.${acc.user.username}', true);
  }

  @override
  Future<Session?> restoreSession() async {
    await _delay();
    final username = _prefs.getString('demo.pin.user');
    final acc = username == null ? null : _accounts[username];
    if (acc == null || _prefs.getString('demo.pin') == null) return null;
    _account = acc;
    final saved = _prefs.getString('demo.hospital');
    _hospitalId = acc.hospitals.any((h) => h.hospitalId == saved) ? saved : acc.hospitals.first.hospitalId;
    return _session();
  }

  @override
  Future<void> setPin(String pin) async {
    await _delay();
    if (_isWeakPin(pin)) {
      _fail(400, 'pin_too_simple', 'รหัส PIN ง่ายเกินไป หลีกเลี่ยงเลขเรียงหรือเลขซ้ำกัน');
    }
    await _prefs.setString('demo.pin', pin);
    await _prefs.setString('demo.pin.user', _me.user.username);
    await _prefs.setBool('demo.pin.temp', false);
    await _prefs.setInt('demo.pin.attempts', 0);
  }

  static bool _isWeakPin(String pin) {
    if (RegExp(r'^(\d)\1+$').hasMatch(pin)) return true;
    const seqs = ['0123456789', '9876543210'];
    return seqs.any((s) => s.contains(pin));
  }

  @override
  Future<PinCheck> verifyPin(String pin) async {
    await _delay();
    final used = _prefs.getInt('demo.pin.attempts') ?? 0;
    if (pin == _prefs.getString('demo.pin')) {
      await _prefs.setInt('demo.pin.attempts', 0);
      return PinCheck(ok: true, attemptsLeft: maxPinAttempts, temporary: _prefs.getBool('demo.pin.temp') ?? false);
    }
    final left = maxPinAttempts - used - 1;
    await _prefs.setInt('demo.pin.attempts', used + 1);
    if (left <= 0) {
      await _clearDevice();
      _fail(423, 'pin_locked', 'ใส่รหัส PIN ผิดเกินจำนวนครั้งที่กำหนด กรุณาเข้าสู่ระบบด้วยรหัสผ่านอีกครั้ง');
    }
    return PinCheck(ok: false, attemptsLeft: left);
  }

  @override
  Future<String> forgotPin() async {
    await _delay();
    await _prefs.setString('demo.pin', demoTempPin);
    await _prefs.setBool('demo.pin.temp', true);
    await _prefs.setInt('demo.pin.attempts', 0);
    return Fmt.maskPhone(_me.user.phone);
  }

  @override
  Future<void> signOut() async {
    await _delay();
    await _clearDevice();
  }

  Future<void> _clearDevice() async {
    _account = null;
    _pending = null;
    for (final k in ['demo.pin', 'demo.pin.user', 'demo.pin.temp', 'demo.pin.attempts', 'demo.hospital']) {
      await _prefs.remove(k);
    }
  }

  @override
  Future<Session> switchHospital(String hospitalId) async {
    await _delay();
    if (!_me.hospitals.any((h) => h.hospitalId == hospitalId)) {
      _fail(403, 'hospital_not_allowed', 'คุณไม่มีสิทธิ์เข้าใช้งานโรงพยาบาลที่เลือก');
    }
    _hospitalId = hospitalId;
    await _prefs.setString('demo.hospital', hospitalId);
    return _session();
  }

  Session _session() {
    final acc = _me;
    return Session(
      token: 'demo-token',
      expiresAt: _now.add(const Duration(hours: 8)),
      user: acc.user,
      hospitalId: _hospitalId!,
      hospitals: acc.hospitals,
      roles: [acc.hospitals.first.roleCode],
      permissions: acc.permissions,
      passwordExpiresAt: _now.add(Duration(days: acc.passwordDaysLeft)),
    );
  }


  bool _passwordOk(_Account a, String password) => password == _passwordOf(a) || password == demoPassword;

  String _passwordOf(_Account a) => _prefs.getString('demo.password.${a.user.username}') ?? demoPassword;

  HospitalAccess get _hospital => _me.hospitals.firstWhere((h) => h.hospitalId == _hospitalId);


  @override
  Future<DoctorDashboard> doctorDashboard() async {
    await _delay();
    final today = Fmt.dayOf(_now);
    final daily = _dailyFor(today.year, today.month);
    final todayIncome = daily.where((d) => Fmt.sameDay(d.date, today)).fold(0.0, (s, d) => s + d.amount);
    final month = daily.fold(0.0, (s, d) => s + d.amount);
    return DoctorDashboard(
      todayIncome: todayIncome,
      monthIncome: month,
      monthLastYearDelta: 0.064,
      asOf: _now,
      today: _attendance[_key(today)] ?? AttendanceDay(date: today),
      todayShifts: _shiftsBetween(today, today).where((s) => s.hospitalNameTh == _hospital.nameTh).toList(),
    );
  }


  @override
  Future<DoctorProfile> doctorProfile() async {
    await _delay();
    return _doctorProfile;
  }

  @override
  Future<List<BankAccount>> bankAccounts() async {
    await _delay();
    return [
      BankAccount(
        id: 'ba-1',
        expenseType: 'PR: Payroll',
        bankNameTh: 'ธนาคารไทยพาณิชย์',
        bankCode: '014',
        branchNameTh: 'สาขาพหลโยธิน',
        branchCode: '0231',
        accountNo: '4052318876',
        accountName: 'นพ.ธนากร วัฒนศิริ',
        active: true,
        effectiveFrom: DateTime(2024, 1, 1),
        documentName: 'bookbank_scb_2024.pdf',
      ),
      BankAccount(
        id: 'ba-0',
        expenseType: 'PR: Payroll',
        bankNameTh: 'ธนาคารกรุงศรีอยุธยา',
        bankCode: '025',
        branchNameTh: 'สาขาราชวิถี',
        branchCode: '0112',
        accountNo: '1239904417',
        accountName: 'นพ.ธนากร วัฒนศิริ',
        active: false,
        effectiveFrom: DateTime(2019, 6, 1),
        documentName: 'bookbank_bay_2019.pdf',
      ),
    ];
  }

  @override
  Future<List<BankOption>> banks() async {
    await _delay();
    return const [
      BankOption('014', 'ธนาคารไทยพาณิชย์', 'SCB'),
      BankOption('025', 'ธนาคารกรุงศรีอยุธยา', 'BAY'),
      BankOption('002', 'ธนาคารกรุงเทพ', 'BBL'),
      BankOption('004', 'ธนาคารกสิกรไทย', 'KBANK'),
      BankOption('006', 'ธนาคารกรุงไทย', 'KTB'),
      BankOption('011', 'ธนาคารทหารไทยธนชาต', 'TTB'),
    ];
  }


  @override
  Future<AttendanceSite> attendanceSite() async {
    await _delay();
    final h = _hospital;
    final loc = _sites[h.hospitalId]!;
    return AttendanceSite(
      hospitalId: h.hospitalId,
      nameTh: h.nameTh,
      nameEn: h.nameEn,
      location: loc,
      radiusM: 300,
      allowOutside: false,
    );
  }


  GeoPoint demoDeviceLocation() {
    final site = _sites[_hospitalId] ?? _sites.values.first;
    return GeoPoint(site.lat + 0.0009, site.lng + 0.0006, accuracyM: 12);
  }

  @override
  Future<AttendanceDay> attendanceToday() async {
    await _delay();
    final today = Fmt.dayOf(_now);
    return _attendance[_key(today)] ?? AttendanceDay(date: today);
  }

  @override
  Future<AttendanceEvent> recordAttendance(AttendanceType type, GeoPoint at) async {
    await _delay();
    final site = await attendanceSite();
    final dist = distanceMeters(site.location, at);
    final inArea = dist <= site.radiusM;
    if (!inArea && !site.allowOutside) {
      _fail(422, 'outside_area',
          'คุณอยู่นอกพื้นที่ลงเวลา (${Fmt.distance(dist, false)} จากโรงพยาบาล) ต้องอยู่ในรัศมี ${site.radiusM.round()} ม.');
    }
    final today = Fmt.dayOf(_now);
    final day = _attendance[_key(today)] ?? AttendanceDay(date: today);
    final event = AttendanceEvent(type: type, at: _now, distanceM: dist, inArea: inArea, siteNameTh: site.nameTh);
    if (type == AttendanceType.checkIn) {
      if (day.checkIn != null) _fail(409, 'already_checked_in', 'คุณลงเวลาเข้างานของวันนี้แล้ว');
      _attendance[_key(today)] = AttendanceDay(date: today, checkIn: event);
    } else {
      if (day.checkIn == null) _fail(409, 'not_checked_in', 'ยังไม่ได้ลงเวลาเข้างานของวันนี้');
      if (day.checkOut != null) _fail(409, 'already_checked_out', 'คุณลงเวลาออกงานของวันนี้แล้ว');
      _attendance[_key(today)] = AttendanceDay(date: today, checkIn: day.checkIn, checkOut: event);
    }
    return event;
  }

  @override
  Future<List<AttendanceDay>> attendanceHistory(int year, int month) async {
    await _delay();
    final days = _attendance.values.where((d) => d.date.year == year && d.date.month == month).toList()
      ..sort((a, b) => b.date.compareTo(a.date));
    return days;
  }


  static double distanceMeters(GeoPoint a, GeoPoint b) {
    const r = 6371000.0;
    double rad(double d) => d * math.pi / 180;
    final dLat = rad(b.lat - a.lat), dLng = rad(b.lng - a.lng);
    final h = math.pow(math.sin(dLat / 2), 2) +
        math.cos(rad(a.lat)) * math.cos(rad(b.lat)) * math.pow(math.sin(dLng / 2), 2);
    return 2 * r * math.asin(math.sqrt(h));
  }


  @override
  Future<List<DutyShift>> shifts(DateTime from, DateTime to) async {
    await _delay();
    return _shiftsBetween(from, to);
  }

  List<DutyShift> _shiftsBetween(DateTime from, DateTime to) {
    final out = <DutyShift>[];
    final today = Fmt.dayOf(_now);
    for (var d = Fmt.dayOf(from); !d.isAfter(to); d = DateTime(d.year, d.month, d.day + 1)) {
      final wd = d.weekday;
      final cancelled = (d.day == 12 || d.day == 27) && wd <= 5;
      ShiftStatus st() => cancelled ? ShiftStatus.cancelled : ShiftStatus.onDuty;
      String? note() => cancelled ? 'ประชุมวิชาการประจำปี' : null;
      if (wd == DateTime.monday || wd == DateTime.wednesday || wd == DateTime.friday || Fmt.sameDay(d, today)) {
        out.add(DutyShift(
          id: 's-${_key(d)}-a',
          date: d,
          startMinute: 9 * 60,
          endMinute: 16 * 60,
          clinicTh: 'คลินิกอายุรกรรมหัวใจ',
          clinicEn: 'Cardiology Clinic',
          room: 'ห้องตรวจ 3',
          hospitalNameTh: 'โรงพยาบาลพญาไท 1',
          status: Fmt.sameDay(d, today) ? ShiftStatus.onDuty : st(),
          note: Fmt.sameDay(d, today) ? null : note(),
        ));
      }
      if (wd == DateTime.tuesday || wd == DateTime.thursday) {
        out.add(DutyShift(
          id: 's-${_key(d)}-b',
          date: d,
          startMinute: 13 * 60,
          endMinute: 20 * 60,
          clinicTh: 'ศูนย์หัวใจ',
          clinicEn: 'Heart Center',
          room: 'ห้องตรวจ 5',
          hospitalNameTh: 'โรงพยาบาลพญาไท 3',
          status: st(),
          note: note(),
        ));
      }
      if (wd == DateTime.saturday && d.day.isEven) {
        out.add(DutyShift(
          id: 's-${_key(d)}-c',
          date: d,
          startMinute: 9 * 60,
          endMinute: 12 * 60,
          clinicTh: 'คลินิกนอกเวลา อายุรกรรม',
          clinicEn: 'After-hours Medicine Clinic',
          room: 'ห้องตรวจ 1',
          hospitalNameTh: 'โรงพยาบาลพญาไท นวมินทร์',
          status: ShiftStatus.onDuty,
        ));
      }
    }
    return out;
  }


  @override
  Future<List<int>> incomeYears() async {
    await _delay();
    return [_now.year, _now.year - 1, _now.year - 2];
  }

  @override
  Future<IncomeYear> incomeYear(int year) async {
    await _delay();
    final lastMonth = year == _now.year ? _now.month : 12;
    final months = [
      for (var m = lastMonth; m >= 1; m--)
        MonthlyIncome(
          year: year,
          month: m,
          net: _slip(year, m).total,
          paidOn: year == _now.year && m == _now.month ? null : DateTime(year, m + 1, 5),
        ),
    ];
    return IncomeYear(
      year: year,
      total: months.fold(0.0, (s, m) => s + m.net),
      asOf: year == _now.year ? _now.subtract(const Duration(days: 1)) : DateTime(year, 12, 31),
      doctorCode: _hospital.doctorCode ?? '-',
      months: months,
    );
  }

  @override
  Future<List<DailyIncome>> dailyIncome(int year, int month) async {
    await _delay();
    return _dailyFor(year, month).reversed.toList();
  }

  List<DailyIncome> _dailyFor(int year, int month) {
    final today = Fmt.dayOf(_now);
    final last = DateTime(year, month + 1, 0);
    final end = last.isAfter(today) ? today : last;
    final shiftDays = _shiftsBetween(DateTime(year, month, 1), end)
        .where((s) => s.status == ShiftStatus.onDuty)
        .map((s) => Fmt.dayOf(s.date))
        .toSet()
        .toList()
      ..sort();
    return [
      for (final d in shiftDays)
        DailyIncome(
          date: d,
          amount: (6000 + 12000 * _rand(d.day, d.month + d.year)).roundToDouble() + 0.5 * (d.day % 2),
          cases: 8 + (17 * _rand(d.month, d.day)).round(),
          confirmed: !Fmt.sameDay(d, today),
        ),
    ];
  }

  @override
  Future<PaySlip> paySlip(int year, int month) async {
    await _delay();
    return _slip(year, month);
  }

  PaySlip _slip(int year, int month) {
    double r(int salt, double lo, double hi) => (lo + (hi - lo) * _rand(year * 12 + month, salt)).roundToDouble();
    final c1 = r(1, 120000, 210000), a1 = r(2, 30000, 60000);
    final c2 = r(3, 8000, 22000), a2 = r(4, 2000, 8000);
    final c3 = r(5, 4000, 12000), a3 = r(6, 1000, 4000);
    final sumC = c1 + c2 + c3, sumA = a1 + a2 + a3;
    final adj406 = month % 4 == 0 ? -r(7, 800, 2500) : 0.0;
    final card = -(sumC * 0.012).roundToDouble();
    final tax406 = -((sumC + adj406) * 0.03).roundToDouble();
    final net406 = sumC + adj406 + card + tax406;

    final hourly = r(8, 12000, 26000), period = r(9, 8000, 18000), monthly = month.isOdd ? 20000.0 : 0.0;
    final surplus = r(10, 3000, 9000), adj402 = month % 3 == 0 ? 1500.0 : 0.0, other = 0.0;
    final inc402 = hourly + period + monthly + surplus + adj402 + other;
    final tax402 = -(inc402 * 0.05).roundToDouble();
    final net402 = inc402 + tax402;

    return PaySlip(
      year: year,
      month: month,
      hospitalNameTh: 'โรงพยาบาลพญาไท 1',
      doctorName: 'นพ.ธนากร วัฒนศิริ',
      paidOn: year == _now.year && month == _now.month ? null : DateTime(year, month + 1, 5),
      sections: [
        PaySlipSection(
          title: '1. สรุปเงินได้ 40(6)',
          columns: const ['เงินสด', 'ลูกหนี้'],
          rows: [
            PaySlipRow('รายได้ 40(6)', [c1, a1]),
            PaySlipRow('รายได้ 40(6) เทียบ (H/P)', [c2, a2]),
            PaySlipRow('รายได้ 40(6) เทียบ (M)', [c3, a3]),
            PaySlipRow('รวมเงินได้แยกประเภท', [sumC, sumA], total: true),
            PaySlipRow('รวมเงินได้ 40(6)', [sumC, null]),
            PaySlipRow('ปรับปรุง 40(6)', [adj406, null]),
            PaySlipRow('ค่าธรรมเนียมบัตรเครดิต', [card, null]),
            PaySlipRow('ภาษีหัก ณ ที่จ่าย', [tax406, null]),
            PaySlipRow('รวม 40(6) สุทธิ', [net406, null], total: true),
          ],
        ),
        PaySlipSection(
          title: '2. สรุปเงินได้ 40(2)',
          columns: const ['เงินได้', 'รายการหัก'],
          rows: [
            PaySlipRow('(+) ประกันรายได้ รายชั่วโมง', [hourly, null]),
            PaySlipRow('(+) ประกันรายได้ รายคาบ', [period, null]),
            PaySlipRow('(+) ประกันรายได้ รายเดือน', [monthly, null]),
            PaySlipRow('ค่าเวร Surplus', [surplus, null]),
            PaySlipRow('ปรับปรุง 40(2)', [adj402, null]),
            PaySlipRow('อื่น ๆ', [other, null]),
            PaySlipRow('ภาษีหัก ณ ที่จ่าย', [null, tax402]),
            PaySlipRow('รวม 40(2) สุทธิ', [net402, null], total: true),
          ],
        ),
        PaySlipSection(
          title: '3. ลูกหนี้ค้างชำระ 40(6)',
          columns: const ['40(6)', 'เทียบ H/P', 'เทียบ M'],
          rows: [
            PaySlipRow(Fmt.monthYear(year, month, false), [a1, a2, a3]),
            PaySlipRow('เดือนก่อนหน้า', [r(11, 5000, 15000), r(12, 500, 2500), r(13, 0, 1200)]),
            PaySlipRow('รวมลูกหนี้ค้างชำระ 40(6)', [a1 + r(11, 5000, 15000), a2 + r(12, 500, 2500), a3 + r(13, 0, 1200)],
                total: true),
          ],
        ),
      ],
      net406: net406,
      net402: net402,
      payType: 'โอนเข้าบัญชี (PFEM)',
      taxId: '3100500123457',
      bankNameTh: 'ธนาคารไทยพาณิชย์',
      accountNo: '4052318876',
      accountName: 'นพ.ธนากร วัฒนศิริ',
      accumulated402Income: inc402 * month,
      accumulated402Tax: -tax402 * month,
    );
  }

  @override
  Future<DocumentRequestRecord> requestDocument(DocumentKind kind, {required int year, int? month, int? toMonth}) async {
    await _delay();
    final (th, en) = switch (kind) {
      DocumentKind.paySlip => (Fmt.monthYear(year, month!, false), Fmt.monthYear(year, month, true)),
      DocumentKind.incomeCertificate => (
          '${Fmt.monthShort(month!, false)} – ${Fmt.monthShort(toMonth!, false)} ${Fmt.year(year, false)}',
          '${Fmt.monthShort(month, true)} – ${Fmt.monthShort(toMonth, true)} $year',
        ),
      DocumentKind.withholdingTax => ('ปีภาษี ${Fmt.year(year, false)}', 'Tax year $year'),
    };
    if (kind == DocumentKind.incomeCertificate && toMonth! < month!) {
      _fail(400, 'invalid_range', 'เดือนสิ้นสุดต้องไม่ก่อนเดือนเริ่มต้น');
    }
    final rec = DocumentRequestRecord(
      id: 'doc-${_seq++}',
      kind: kind,
      periodLabelTh: th,
      periodLabelEn: en,
      requestedAt: _now,
      email: _me.user.email,
    );
    _documents.insert(0, rec);
    return rec;
  }

  @override
  Future<List<DocumentRequestRecord>> documentHistory() async {
    await _delay();
    return List.unmodifiable(_documents);
  }


  String get _myRole => _hospital.roleCode;
  String get _myName => _me.user.displayNameTh;

  @override
  Future<List<ApprovalRequest>> myRequests() async {
    await _delay();
    return _requests.where((r) => r.requestedBy == _myName).toList()
      ..sort((a, b) => b.requestedAt.compareTo(a.requestedAt));
  }

  @override
  Future<List<ApprovalRequest>> pendingApprovals() async {
    await _delay();
    if (!_session().canApprove) return const [];

    return _requests
        .where((r) =>
            r.status == ApprovalStatus.pending &&
            _stepRoleCode(r.requestType) == _myRole &&
            r.requestedBy != _myName)
        .toList()
      ..sort((a, b) => b.requestedAt.compareTo(a.requestedAt));
  }

  @override
  Future<List<ApprovalRequest>> approvalHistory() async {
    await _delay();
    return _requests.where((r) {
      if (r.status == ApprovalStatus.pending) return false;
      if (r.requestedBy == _myName) return true;
      return _details[r.id]!.steps.any((s) => s.user == _myName);
    }).toList()
      ..sort((a, b) => b.updatedAt.compareTo(a.updatedAt));
  }

  @override
  Future<ApprovalDetail> approvalDetail(String id) async {
    await _delay();
    final d = _details[id] ?? _fail(404, 'not_found', 'ไม่พบคำขอนี้ หรือคำขอถูกยกเลิกแล้ว');
    final canDecide = d.request.status == ApprovalStatus.pending &&
        _session().canApprove &&
        _stepRoleCode(d.request.requestType) == _myRole &&
        d.request.requestedBy != _myName;
    return ApprovalDetail(
      request: d.request,
      doctorName: d.doctorName,
      reason: d.reason,
      changes: d.changes,
      attachments: d.attachments,
      steps: d.steps,
      canDecide: canDecide,
    );
  }

  @override
  Future<void> decide(List<String> ids, Decision decision, {String? comment}) async {
    await _delay();
    if (decision == Decision.reject && (comment == null || comment.trim().isEmpty)) {
      _fail(400, 'comment_required', 'กรุณาระบุเหตุผลที่ไม่อนุมัติ');
    }

    for (final id in ids) {
      final d = _details[id];
      if (d == null || d.request.status != ApprovalStatus.pending) {
        _fail(409, 'already_decided', 'มีบางรายการถูกดำเนินการไปแล้ว กรุณารีเฟรชรายการ');
      }
      if (d.request.requestedBy == _myName) {
        _fail(403, 'self_approval', 'ไม่สามารถพิจารณาคำขอของตัวเองได้');
      }
    }
    for (final id in ids) {
      final d = _details[id]!;
      final status = decision == Decision.approve ? ApprovalStatus.approved : ApprovalStatus.rejected;
      final req = d.request.copyWith(status: status, updatedAt: _now);
      _requests[_requests.indexWhere((r) => r.id == id)] = req;
      _details[id] = ApprovalDetail(
        request: req,
        doctorName: d.doctorName,
        reason: d.reason,
        changes: d.changes,
        attachments: d.attachments,
        steps: [
          for (final s in d.steps)
            s.action == null
                ? ApprovalStep(
                    seq: s.seq,
                    roleNameTh: s.roleNameTh,
                    user: _myName,
                    action: decision == Decision.approve ? StepAction.approve : StepAction.reject,
                    at: _now,
                    comment: comment?.trim().isEmpty ?? true ? null : comment!.trim(),
                  )
                : s,
        ],
        canDecide: false,
      );
      if (d.doctorName == _doctorAccount.user.displayNameTh) {
        _notify(_doctorAccount.user.username, AppNotification(
          id: 'n-${_seq++}',
          kind: NotificationKind.approval,
          titleTh: decision == Decision.approve ? 'คำขอได้รับการอนุมัติ' : 'คำขอไม่ได้รับการอนุมัติ',
          titleEn: decision == Decision.approve ? 'Request approved' : 'Request rejected',
          bodyTh: '${req.requestNo} · ${req.summary}',
          bodyEn: '${req.requestNo} · ${req.summary}',
          at: _now,
          read: false,
          targetId: id,
        ));
      }
    }
  }

  @override
  Future<ApprovalRequest> submitEditRequest(EditRequestDraft draft) async {
    await _delay();
    if (draft.changes.isEmpty) _fail(400, 'no_changes', 'กรุณาแก้ไขข้อมูลอย่างน้อยหนึ่งรายการ');
    final prefix = draft.requestType == RequestType.bankAccount ? 'BA' : 'DP';
    final summary = draft.requestType == RequestType.bankAccount
        ? 'ขอแก้ไขบัญชีธนาคารสำหรับรับเงิน'
        : 'ขอแก้ไข${draft.changes.map((c) => c.label).take(2).join('และ')}';
    final id = 'r-${_seq++}';
    final req = ApprovalRequest(
      id: id,
      requestNo: '$prefix-${Fmt.year(_now.year, false) % 100}${_now.month.toString().padLeft(2, '0')}-${(_seq * 37 % 9000 + 1000)}',
      requestType: draft.requestType,
      summary: summary,
      requestedBy: _myName,
      requestedAt: _now,
      updatedAt: _now,
      status: ApprovalStatus.pending,
      currentStepRoleTh: _stepRoleName(draft.requestType),
    );
    _requests.insert(0, req);
    _details[id] = ApprovalDetail(
      request: req,
      doctorName: _myName,
      reason: draft.reason,
      changes: draft.changes,
      attachments: draft.attachments,
      steps: [ApprovalStep(seq: 1, roleNameTh: _stepRoleName(draft.requestType))],
      canDecide: false,
    );
    return req;
  }

  static String _stepRoleCode(String type) =>
      type == RequestType.doctorProfile || type == RequestType.specialty ? 'MD_OFFICE' : 'DOCTOR_ACCOUNTING';

  static String _stepRoleName(String type) =>
      _stepRoleCode(type) == 'MD_OFFICE' ? 'สำนักผู้อำนวยการแพทย์' : 'บัญชีแพทย์';


  void _notify(String username, AppNotification n) => (_notifications[username] ??= []).insert(0, n);

  @override
  Future<List<AppNotification>> notifications() async {
    await _delay();
    return List.unmodifiable(_notifications[_me.user.username] ?? const []);
  }

  @override
  Future<void> markNotificationRead(String id) async {
    final list = _notifications[_me.user.username];
    if (list == null) return;
    final i = list.indexWhere((n) => n.id == id);
    if (i >= 0) list[i] = list[i].markRead();
  }

  @override
  Future<void> markAllNotificationsRead() async {
    await _delay();
    final list = _notifications[_me.user.username];
    if (list == null) return;
    for (var i = 0; i < list.length; i++) {
      list[i] = list[i].markRead();
    }
  }


  static String _key(DateTime d) => '${d.year}-${d.month}-${d.day}';


  static double _rand(int a, int b) {
    var h = (a * 374761393 + b * 668265263) & 0x7fffffff;
    h = ((h ^ (h >> 13)) * 1274126177) & 0x7fffffff;
    return (h % 10000) / 10000;
  }

  static const _sites = {
    'PT1': GeoPoint(13.76498, 100.53781),
    'PT3': GeoPoint(13.72218, 100.46372),
    'PTN': GeoPoint(13.82011, 100.66602),
  };

  static final _doctorAccount = _Account(
    user: const SessionUser(
      id: 'u-d10001',
      username: 'D10001',
      displayNameTh: 'นพ.ธนากร วัฒนศิริ',
      displayNameEn: 'Dr. Thanakorn Wattanasiri',
      positionTh: 'อายุรแพทย์โรคหัวใจ',
      positionEn: 'Cardiologist',
      email: 'thanakorn.w@phyathai.com',
      phone: '0812345521',
    ),
    hospitals: const [
      HospitalAccess(
        hospitalId: 'PT1',
        nameTh: 'โรงพยาบาลพญาไท 1',
        nameEn: 'Phyathai 1 Hospital',
        roleCode: 'DOCTOR',
        roleNameTh: 'แพทย์',
        roleNameEn: 'Doctor',
        doctorCode: 'PT1-10001-00',
      ),
      HospitalAccess(
        hospitalId: 'PT3',
        nameTh: 'โรงพยาบาลพญาไท 3',
        nameEn: 'Phyathai 3 Hospital',
        roleCode: 'DOCTOR',
        roleNameTh: 'แพทย์',
        roleNameEn: 'Doctor',
        doctorCode: 'PT3-10001-00',
      ),
      HospitalAccess(
        hospitalId: 'PTN',
        nameTh: 'โรงพยาบาลพญาไท นวมินทร์',
        nameEn: 'Phyathai Nawamin Hospital',
        roleCode: 'DOCTOR',
        roleNameTh: 'แพทย์',
        roleNameEn: 'Doctor',
        doctorCode: 'PTN-10001-02',
      ),
    ],
    permissions: const ['approvals.read', 'income.read', 'attendance.write', 'schedule.read'],
    passwordDaysLeft: 62,
  );

  static final _accounts = <String, _Account>{
    'D10001': _doctorAccount,
    'A20001': _Account(
      user: const SessionUser(
        id: 'u-a20001',
        username: 'A20001',
        displayNameTh: 'คุณศิริพร มั่นคง',
        displayNameEn: 'Siriporn Mankong',
        positionTh: 'เจ้าหน้าที่บัญชีแพทย์',
        positionEn: 'Doctor Accounting Officer',
        email: 'siriporn.m@phyathai.com',
        phone: '0898765012',
      ),
      hospitals: const [
        HospitalAccess(
          hospitalId: 'PT1',
          nameTh: 'โรงพยาบาลพญาไท 1',
          nameEn: 'Phyathai 1 Hospital',
          roleCode: 'DOCTOR_ACCOUNTING',
          roleNameTh: 'บัญชีแพทย์',
          roleNameEn: 'Doctor Accounting',
        ),
        HospitalAccess(
          hospitalId: 'PT3',
          nameTh: 'โรงพยาบาลพญาไท 3',
          nameEn: 'Phyathai 3 Hospital',
          roleCode: 'DOCTOR_ACCOUNTING',
          roleNameTh: 'บัญชีแพทย์',
          roleNameEn: 'Doctor Accounting',
        ),
      ],
      permissions: const ['approvals.read', 'approvals.approve'],
      passwordDaysLeft: 12,
    ),
    'M30001': _Account(
      user: const SessionUser(
        id: 'u-m30001',
        username: 'M30001',
        displayNameTh: 'คุณวิภาวรรณ ทองดี',
        displayNameEn: 'Wipawan Thongdee',
        positionTh: 'เจ้าหน้าที่สำนักผู้อำนวยการแพทย์',
        positionEn: 'Medical Director Office',
        email: 'wipawan.t@phyathai.com',
        phone: '0861112093',
      ),
      hospitals: const [
        HospitalAccess(
          hospitalId: 'PT1',
          nameTh: 'โรงพยาบาลพญาไท 1',
          nameEn: 'Phyathai 1 Hospital',
          roleCode: 'MD_OFFICE',
          roleNameTh: 'สำนักผู้อำนวยการแพทย์',
          roleNameEn: 'Medical Director Office',
        ),
      ],
      permissions: const ['approvals.read', 'approvals.approve'],
      passwordDaysLeft: 140,
    ),
  };

  static final _doctorProfile = DoctorProfile(
    prefixTh: 'นพ.',
    prefixEn: 'Dr.',
    firstNameTh: 'ธนากร',
    lastNameTh: 'วัฒนศิริ',
    firstNameEn: 'Thanakorn',
    lastNameEn: 'Wattanasiri',
    nationalId: '3100500123457',
    birthDate: DateTime(1981, 12, 7),
    passportNo: 'AA1234567',
    passportExpiry: DateTime(2029, 3, 14),
    phone: '0812345521',
    email: 'thanakorn.w@phyathai.com',
    address: '99/12 ถนนพหลโยธิน แขวงสามเสนใน เขตพญาไท กรุงเทพฯ 10400',
    licenseNo: 'ว.31245',
    licenseExpiry: DateTime(2027, 11, 30),
    specialty: 'อายุรศาสตร์',
    subSpecialty: 'อายุรศาสตร์โรคหัวใจ',
    department: 'ศูนย์หัวใจ',
    education: const [
      Education('แพทยศาสตรบัณฑิต', 'มหาวิทยาลัยมหิดล', 2006),
      Education('วุฒิบัตรอายุรศาสตร์', 'แพทยสภา', 2010),
      Education('อนุมัติบัตรอายุรศาสตร์โรคหัวใจ', 'แพทยสภา', 2013),
    ],
    codes: const [
      DoctorCodeInfo('โรงพยาบาลพญาไท 1', 'PT1-10001-00', 'แพทย์ประจำ (Full-time)', true),
      DoctorCodeInfo('โรงพยาบาลพญาไท 3', 'PT3-10001-00', 'แพทย์ Part-time', true),
      DoctorCodeInfo('โรงพยาบาลพญาไท นวมินทร์', 'PTN-10001-02', 'แพทย์ Part-time', true),
    ],
    documents: [
      DoctorDocument(nameTh: 'สำเนาบัตรประชาชน', fileName: 'id_card.pdf', uploadedAt: DateTime(2024, 1, 8), sizeKb: 412),
      DoctorDocument(
        nameTh: 'ใบอนุญาตประกอบวิชาชีพเวชกรรม',
        fileName: 'medical_license.pdf',
        uploadedAt: DateTime(2024, 1, 8),
        sizeKb: 856,
        expiresAt: DateTime(2027, 11, 30),
      ),
      DoctorDocument(nameTh: 'วุฒิบัตรอายุรศาสตร์', fileName: 'board_cert.pdf', uploadedAt: DateTime(2024, 1, 8), sizeKb: 1320),
      DoctorDocument(nameTh: 'สำเนาหน้าสมุดบัญชี', fileName: 'bookbank_scb_2024.pdf', uploadedAt: DateTime(2024, 1, 2), sizeKb: 298),
    ],
  );

  void _seed() {
    final now = _now;
    final today = Fmt.dayOf(now);


    for (final s in _shiftsBetween(today.subtract(const Duration(days: 45)), today.subtract(const Duration(days: 1)))) {
      if (s.status != ShiftStatus.onDuty || _attendance.containsKey(_key(s.date))) continue;
      final inAt = s.start.subtract(Duration(minutes: 4 + (14 * _rand(s.date.day, 3)).round()));
      final outAt = s.end.add(Duration(minutes: 2 + (40 * _rand(s.date.day, 5)).round()));
      final far = s.date.day == 9;
      _attendance[_key(s.date)] = AttendanceDay(
        date: s.date,
        checkIn: AttendanceEvent(
            type: AttendanceType.checkIn, at: inAt, distanceM: 40 + 90 * _rand(s.date.day, 1), inArea: true,
            siteNameTh: s.hospitalNameTh),
        checkOut: AttendanceEvent(
            type: AttendanceType.checkOut, at: outAt, distanceM: far ? 420 : 60 + 80 * _rand(s.date.day, 2),
            inArea: !far, siteNameTh: s.hospitalNameTh),
      );
    }

    DateTime ago(int days, int h, int m) => DateTime(today.year, today.month, today.day - days, h, m);
    var n = 0;
    void add(String type, String prefix, String summary, String by, DateTime at, ApprovalStatus status,
        {required List<FieldChange> changes, String? reason, List<Attachment> attachments = const [],
        String? decidedBy, String? comment}) {
      final id = 'r-${n++}';
      final role = _stepRoleName(type);
      final req = ApprovalRequest(
        id: id,
        requestNo: '$prefix-${Fmt.year(at.year, false) % 100}${at.month.toString().padLeft(2, '0')}-${1021 + n * 7}',
        requestType: type,
        summary: summary,
        requestedBy: by,
        requestedAt: at,
        updatedAt: status == ApprovalStatus.pending ? at : at.add(const Duration(hours: 5)),
        status: status,
        currentStepRoleTh: status == ApprovalStatus.pending ? role : null,
      );
      _requests.add(req);
      _details[id] = ApprovalDetail(
        request: req,
        doctorName: by,
        reason: reason,
        changes: changes,
        attachments: attachments,
        steps: [
          ApprovalStep(
            seq: 1,
            roleNameTh: role,
            user: status == ApprovalStatus.pending ? null : decidedBy,
            action: switch (status) {
              ApprovalStatus.approved => StepAction.approve,
              ApprovalStatus.rejected => StepAction.reject,
              ApprovalStatus.returned => StepAction.returned,
              _ => null,
            },
            at: status == ApprovalStatus.pending ? null : req.updatedAt,
            comment: comment,
          ),
        ],
        canDecide: false,
      );
    }

    const me = 'นพ.ธนากร วัฒนศิริ';
    add(RequestType.doctorProfile, 'DP', 'ขอแก้ไขเบอร์โทรศัพท์และที่อยู่', me, ago(0, 10, 30), ApprovalStatus.pending,
        reason: 'ย้ายที่อยู่และเปลี่ยนเบอร์ติดต่อ',
        changes: const [
          FieldChange('เบอร์โทรศัพท์', '081-234-5521', '089-555-0142'),
          FieldChange('ที่อยู่', '99/12 ถนนพหลโยธิน แขวงสามเสนใน เขตพญาไท กรุงเทพฯ 10400',
              '55/8 ซอยอารีย์ 4 แขวงพญาไท เขตพญาไท กรุงเทพฯ 10400'),
        ],
        attachments: const [Attachment('ทะเบียนบ้าน.pdf', 512)]);
    add(RequestType.doctorProfile, 'DP', 'ขอแก้ไขอีเมล', me, ago(18, 8, 30), ApprovalStatus.approved,
        changes: const [FieldChange('อีเมล', 'thanakorn.w@gmail.com', 'thanakorn.w@phyathai.com')],
        decidedBy: 'คุณวิภาวรรณ ทองดี', comment: 'ตรวจสอบเอกสารแล้ว');
    add(RequestType.bankAccount, 'BA', 'ขอแก้ไขบัญชีธนาคารสำหรับรับเงิน', me, ago(40, 14, 5), ApprovalStatus.rejected,
        changes: const [
          FieldChange('ธนาคาร', 'ธนาคารไทยพาณิชย์ (014)', 'ธนาคารกสิกรไทย (004)'),
          FieldChange('เลขที่บัญชี', 'XXX-X-X887-6', 'XXX-X-X102-3'),
        ],
        attachments: const [Attachment('bookbank_kbank.jpg', 780)],
        decidedBy: 'คุณศิริพร มั่นคง',
        comment: 'ชื่อบัญชีในสำเนาสมุดบัญชีไม่ตรงกับชื่อแพทย์ กรุณาแนบสำเนาใหม่');

    add(RequestType.bankAccount, 'BA', 'ขอแก้ไขบัญชีธนาคารสำหรับรับเงิน', 'พญ.กมลชนก ศรีวงศ์', ago(0, 9, 12),
        ApprovalStatus.pending,
        reason: 'เปลี่ยนธนาคารรับเงินเดือน',
        changes: const [
          FieldChange('ธนาคาร', 'ธนาคารกรุงเทพ (002)', 'ธนาคารกรุงไทย (006)'),
          FieldChange('สาขา', 'สาขาสีลม (0101)', 'สาขาพญาไท (0034)'),
          FieldChange('เลขที่บัญชี', 'XXX-X-X455-1', 'XXX-X-X318-7'),
        ],
        attachments: const [Attachment('bookbank_ktb.pdf', 344)]);
    add(RequestType.doctorCode, 'DC', 'ขอเปิดรหัสแพทย์ที่ รพ.พญาไท 3', 'นพ.ภาณุพงศ์ เกียรติไกร', ago(1, 16, 40),
        ApprovalStatus.pending,
        changes: const [
          FieldChange('โรงพยาบาล', '-', 'โรงพยาบาลพญาไท 3'),
          FieldChange('ประเภทแพทย์', '-', 'แพทย์ Part-time'),
        ],
        attachments: const [Attachment('สัญญาจ้าง_PT3.pdf', 1024)]);
    add(RequestType.welfare, 'WF', 'ขอใช้สิทธิ์สวัสดิการค่ารักษาพยาบาล', 'พญ.ณัฐธิดา อินทร์แก้ว', ago(2, 11, 5),
        ApprovalStatus.pending,
        changes: const [FieldChange('วงเงินที่ขอเบิก', '-', '12,500.00')],
        attachments: const [Attachment('ใบเสร็จ.pdf', 220)]);
    add(RequestType.contract, 'CT', 'ขอต่ออายุสัญญาแพทย์', 'นพ.วรเมธ จันทร์หอม', ago(3, 13, 20), ApprovalStatus.pending,
        changes: const [FieldChange('วันสิ้นสุดสัญญา', '31/12/2569', '31/12/2570')]);
    add(RequestType.doctorProfile, 'DP', 'ขอแก้ไขชื่อ-นามสกุล (EN)', 'พญ.กมลชนก ศรีวงศ์', ago(1, 9, 50),
        ApprovalStatus.pending,
        reason: 'สะกดชื่อภาษาอังกฤษตามหนังสือเดินทาง',
        changes: const [FieldChange('ชื่อ - นามสกุล (EN)', 'Kamonchanok Sriwong', 'Kamolchanok Sriwongse')],
        attachments: const [Attachment('passport.jpg', 690)]);
    add(RequestType.specialty, 'SP', 'ขอเพิ่มความเชี่ยวชาญ', 'นพ.วรเมธ จันทร์หอม', ago(4, 15, 0), ApprovalStatus.pending,
        changes: const [FieldChange('ความเชี่ยวชาญ', 'ศัลยศาสตร์', 'ศัลยศาสตร์, ศัลยศาสตร์ส่องกล้อง')],
        attachments: const [Attachment('อนุมัติบัตร.pdf', 910)]);
    add(RequestType.bankAccount, 'BA', 'ขอแก้ไขชื่อบัญชีผู้รับเงิน', 'นพ.ภาณุพงศ์ เกียรติไกร', ago(9, 10, 0),
        ApprovalStatus.approved,
        changes: const [FieldChange('ชื่อบัญชี', 'ภาณุพงศ์ เกียรติไกร', 'นพ.ภาณุพงศ์ เกียรติไกร')],
        decidedBy: 'คุณศิริพร มั่นคง');
    add(RequestType.contract, 'GM', 'การสร้างข้อมูลอัตราประกันรายได้แพทย์ รายเดือน', 'คุณศิริพร มั่นคง', ago(0, 8, 45),
        ApprovalStatus.pending,
        changes: const [FieldChange('อัตราประกันรายได้', '-', '120,000.00 / เดือน')]);

    final m = today.month == 1 ? 12 : today.month - 1;
    final y = today.month == 1 ? today.year - 1 : today.year;
    _notifications['D10001'] = [
      AppNotification(
        id: 'n-1', kind: NotificationKind.approval, at: ago(0, 11, 2), read: false, targetId: 'r-0',
        titleTh: 'ส่งคำขอแก้ไขข้อมูลแล้ว', titleEn: 'Edit request submitted',
        bodyTh: 'คำขอแก้ไขเบอร์โทรศัพท์และที่อยู่ รอสำนักผู้อำนวยการแพทย์พิจารณา',
        bodyEn: 'Your phone and address change is waiting for the Medical Director Office.',
      ),
      AppNotification(
        id: 'n-2', kind: NotificationKind.income, at: ago(1, 7, 30), read: false, targetId: '$y-$m',
        titleTh: 'ใบแจ้งรายได้พร้อมแล้ว', titleEn: 'Pay slip ready',
        bodyTh: 'ใบแจ้งรายได้ประจำเดือน ${Fmt.monthYear(y, m, false)} ดูได้ในแอปแล้ว',
        bodyEn: 'Your pay slip for ${Fmt.monthYear(y, m, true)} is available.',
      ),
      AppNotification(
        id: 'n-3', kind: NotificationKind.schedule, at: ago(2, 17, 10), read: false,
        titleTh: 'ตารางเวรเดือนถัดไปประกาศแล้ว', titleEn: 'Next month schedule published',
        bodyTh: 'ตรวจสอบวันออกตรวจและวันงดตรวจของคุณได้ที่เมนูตารางเวร',
        bodyEn: 'Review your clinic days and cancellations in Duty schedule.',
      ),
      AppNotification(
        id: 'n-4', kind: NotificationKind.approval, at: ago(18, 13, 32), read: true, targetId: 'r-1',
        titleTh: 'คำขอได้รับการอนุมัติ', titleEn: 'Request approved',
        bodyTh: 'คำขอแก้ไขอีเมลได้รับการอนุมัติแล้ว', bodyEn: 'Your email change was approved.',
      ),
      AppNotification(
        id: 'n-5', kind: NotificationKind.security, at: ago(20, 21, 4), read: true,
        titleTh: 'เข้าสู่ระบบจากอุปกรณ์ใหม่', titleEn: 'New device sign-in',
        bodyTh: 'หากไม่ใช่คุณ กรุณาเปลี่ยนรหัสผ่านทันที', bodyEn: "If this wasn't you, change your password now.",
      ),
    ];
    _notifications['A20001'] = [
      AppNotification(
        id: 'n-11', kind: NotificationKind.approval, at: ago(0, 9, 13), read: false, targetId: 'r-3',
        titleTh: 'มีคำขอใหม่รออนุมัติ', titleEn: 'New request to review',
        bodyTh: 'พญ.กมลชนก ศรีวงศ์ ขอแก้ไขบัญชีธนาคารสำหรับรับเงิน',
        bodyEn: 'Kamonchanok Sriwong requested a bank account change.',
      ),
      AppNotification(
        id: 'n-12', kind: NotificationKind.approval, at: ago(1, 16, 41), read: false, targetId: 'r-4',
        titleTh: 'มีคำขอใหม่รออนุมัติ', titleEn: 'New request to review',
        bodyTh: 'นพ.ภาณุพงศ์ เกียรติไกร ขอเปิดรหัสแพทย์ที่ รพ.พญาไท 3',
        bodyEn: 'Panupong Kiatkrai requested a new doctor code at Phyathai 3.',
      ),
      AppNotification(
        id: 'n-13', kind: NotificationKind.security, at: ago(6, 8, 0), read: true,
        titleTh: 'รหัสผ่านใกล้หมดอายุ', titleEn: 'Password expiring soon',
        bodyTh: 'รหัสผ่านของคุณจะหมดอายุในอีก 12 วัน', bodyEn: 'Your password expires in 12 days.',
      ),
    ];
    _notifications['M30001'] = [
      AppNotification(
        id: 'n-21', kind: NotificationKind.approval, at: ago(0, 10, 31), read: false, targetId: 'r-0',
        titleTh: 'มีคำขอใหม่รออนุมัติ', titleEn: 'New request to review',
        bodyTh: 'นพ.ธนากร วัฒนศิริ ขอแก้ไขเบอร์โทรศัพท์และที่อยู่',
        bodyEn: 'Thanakorn Wattanasiri requested a phone and address change.',
      ),
    ];

    _documents.addAll([
      DocumentRequestRecord(
        id: 'doc-1', kind: DocumentKind.withholdingTax, periodLabelTh: 'ปีภาษี ${Fmt.year(today.year - 1, false)}',
        periodLabelEn: 'Tax year ${today.year - 1}', requestedAt: ago(30, 20, 12), email: 'thanakorn.w@phyathai.com',
      ),
      DocumentRequestRecord(
        id: 'doc-2', kind: DocumentKind.paySlip, periodLabelTh: Fmt.monthYear(y, m, false),
        periodLabelEn: Fmt.monthYear(y, m, true), requestedAt: ago(1, 8, 2), email: 'thanakorn.w@phyathai.com',
      ),
    ]);
  }

  static const _termsSections = [
    TermsSection('1. บทนำ',
        'ข้อกำหนดและเงื่อนไขฉบับนี้ใช้กับการใช้งานแอปพลิเคชัน iDA (Intelligent Doctor Application) ของเครือโรงพยาบาลพญาไท-เปาโล '
            'ซึ่งให้บริการแก่แพทย์และเจ้าหน้าที่ที่ได้รับอนุญาตเท่านั้น การเข้าสู่ระบบถือว่าท่านได้อ่านและยอมรับข้อกำหนดทั้งหมด'),
    TermsSection('2. สิทธิในทรัพย์สินทางปัญญา',
        'เนื้อหา ซอฟต์แวร์ เครื่องหมายการค้า และข้อมูลทั้งหมดในแพลตฟอร์มเป็นกรรมสิทธิ์ของบริษัท '
            'ห้ามทำซ้ำ ดัดแปลง หรือเผยแพร่โดยไม่ได้รับอนุญาตเป็นลายลักษณ์อักษร'),
    TermsSection('3. บัญชีผู้ใช้และความปลอดภัย',
        'ท่านต้องเก็บรักษารหัสผ่าน รหัส PIN และรหัส OTP เป็นความลับ ห้ามเปิดเผยแก่ผู้อื่น '
            'รหัสผ่านต้องยาวอย่างน้อย 8 ตัวอักษร ประกอบด้วยตัวพิมพ์ใหญ่ ตัวพิมพ์เล็ก ตัวเลข และอักขระพิเศษ '
            'และต้องเปลี่ยนตามรอบที่โรงพยาบาลกำหนด หากสงสัยว่าบัญชีถูกใช้งานโดยไม่ได้รับอนุญาต กรุณาแจ้งผู้ดูแลระบบทันที'),
    TermsSection('4. การคุ้มครองข้อมูลส่วนบุคคล',
        'บริษัทเก็บรวบรวม ใช้ และเปิดเผยข้อมูลส่วนบุคคลของท่านตามพระราชบัญญัติคุ้มครองข้อมูลส่วนบุคคล พ.ศ. 2562 '
            'เพื่อวัตถุประสงค์ในการบริหารค่าตอบแทนแพทย์ การลงเวลาปฏิบัติงาน และการออกเอกสารทางภาษีเท่านั้น '
            'ข้อมูลตำแหน่งที่ตั้งจะถูกใช้เฉพาะขณะลงเวลาเข้า-ออกงาน'),
    TermsSection('5. การลงเวลาปฏิบัติงาน',
        'การลงเวลาเข้า-ออกงานผ่านแอปใช้ตำแหน่ง GPS ของอุปกรณ์ และต้องอยู่ภายในรัศมีที่โรงพยาบาลกำหนด '
            'การปลอมแปลงตำแหน่งหรือให้ผู้อื่นลงเวลาแทนถือเป็นการละเมิดข้อกำหนดอย่างร้ายแรง'),
    TermsSection('6. ข้อมูลรายได้และเอกสารภาษี',
        'ข้อมูลรายได้ที่แสดงในแอปเป็นข้อมูล ณ วันที่ระบุ ยอดของวันปัจจุบันเป็นยอดประมาณการจนกว่าจะปิดรอบ '
            'เอกสารที่ส่งทางอีเมลเข้ารหัสด้วยวันเดือนปีเกิดของท่าน (DDMMYYYY) ท่านมีหน้าที่เก็บรักษาเอกสารให้ปลอดภัย'),
    TermsSection('7. อภิธานศัพท์',
        '7.1 "แพลตฟอร์ม" หมายถึง แอปพลิเคชัน iDA และระบบที่เกี่ยวข้อง\n'
            '7.2 "ผู้ใช้" หมายถึง แพทย์และเจ้าหน้าที่ที่ได้รับสิทธิ์เข้าใช้งาน\n'
            '7.3 "บริการ" หมายถึง ฟังก์ชันทั้งหมดที่ให้บริการผ่านแพลตฟอร์ม\n'
            '7.4 "เนื้อหา" หมายถึง ข้อมูล ข้อความ และเอกสารที่แสดงในแพลตฟอร์ม\n'
            '7.5 "ผู้ให้บริการ", "บริษัท" หรือ "เรา" หมายถึง เครือโรงพยาบาลพญาไท-เปาโล'),
  ];
}

class _Account {
  const _Account({
    required this.user,
    required this.hospitals,
    required this.permissions,
    required this.passwordDaysLeft,
  });

  final SessionUser user;
  final List<HospitalAccess> hospitals;
  final List<String> permissions;
  final int passwordDaysLeft;
}

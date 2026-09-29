import 'package:flutter_test/flutter_test.dart';
import 'package:ida_mobile/core/format.dart';
import 'package:ida_mobile/data/demo_repository.dart';
import 'package:ida_mobile/data/models.dart';
import 'package:ida_mobile/data/repository.dart';
import 'package:ida_mobile/ui/security_inputs.dart';
import 'package:shared_preferences/shared_preferences.dart';

void main() {
  group('Fmt — DS-IDA §2.4 และ PDPA', () {
    test('เงิน: คั่นหลักพัน ทศนิยม 2 ตำแหน่ง ติดลบเป็นวงเล็บ', () {
      expect(Fmt.money(1234567.891), '1,234,567.89');
      expect(Fmt.money(-1250), '(1,250.00)');
      expect(Fmt.money(0), '0.00');
      expect(Fmt.money(999.5, symbol: true), '฿999.50');
      expect(Fmt.money(-42, symbol: true), '(฿42.00)');
    });

    test('วันที่ภาษาไทยเป็น พ.ศ. ภาษาอังกฤษเป็น ค.ศ.', () {
      final d = DateTime(2026, 9, 25, 8, 5);
      expect(Fmt.date(d, false), '25 ก.ย. 2569');
      expect(Fmt.date(d, true), '25 Sep 2026');
      expect(Fmt.dateFull(d, false), 'วันศุกร์ที่ 25 กันยายน 2569');
      expect(Fmt.time(d), '08:05');
      expect(Fmt.dateNumeric(d, false), '25/09/2569');
    });

    test('วันนี้/เมื่อวาน', () {
      final now = DateTime(2026, 9, 25, 12);
      expect(Fmt.relative(DateTime(2026, 9, 25, 10, 30), false, now: now), 'วันนี้ 10:30');
      expect(Fmt.relative(DateTime(2026, 9, 24, 8, 15), true, now: now), 'Yesterday 08:15');
    });

    test('ปกปิดข้อมูลส่วนบุคคล', () {
      expect(Fmt.maskPhone('0812345521'), '081-XXX-5521');
      expect(Fmt.maskAccount('4052318876'), 'XXX-X-X887-6');
      expect(Fmt.maskNationalId('3100500123457'), '3-1005-XXXXX-XX-7');
      expect(Fmt.formatNationalId('3100500123457'), '3-1005-00123-45-7');
      expect(Fmt.maskEmail('thanakorn.w@phyathai.com'), 'th••••••••@phyathai.com');
    });

    test('ระยะทาง', () {
      expect(Fmt.distance(120.4, false), '120 ม.');
      expect(Fmt.distance(2400, true), '2.4 km');
    });
  });

  group('PasswordPolicy — TOR-DA-MOBILE §6.1.6', () {
    test('ต้องครบทุกข้อและตรงกัน', () {
      expect(const PasswordPolicy('Ida@2026x', 'Ida@2026x').valid, isTrue);
      expect(const PasswordPolicy('ida@2026x', 'ida@2026x').upper, isFalse);
      expect(const PasswordPolicy('IDA@2026X', 'IDA@2026X').lower, isFalse);
      expect(const PasswordPolicy('Ida@abcde', 'Ida@abcde').digit, isFalse);
      expect(const PasswordPolicy('Ida2026xx', 'Ida2026xx').special, isFalse);
      expect(const PasswordPolicy('Id@1', 'Id@1').length, isFalse);
      expect(const PasswordPolicy('Ida@2026x', 'Ida@2026y').valid, isFalse);

      expect(const PasswordPolicy('VeryLong@Password2026', 'VeryLong@Password2026').valid, isTrue);
    });
  });

  group('DemoRepository', () {
    late DemoRepository repo;

    setUp(() async {
      SharedPreferences.setMockInitialValues({});
      repo = DemoRepository(await SharedPreferences.getInstance(), latency: Duration.zero);
    });

    Future<SignInResult> signIn(String user) async {
      final c = await repo.signIn(user, DemoRepository.demoPassword);
      return repo.verifySignInOtp(c.id, DemoRepository.demoOtp);
    }

    test('เปลี่ยนรหัสผ่านแล้ว ทั้งรหัสใหม่และรหัสสาธิตยังเข้าได้ · รหัสผิดยังเข้าไม่ได้', () async {
      await signIn('D10001');
      await repo.changePassword(newPassword: 'Doctor@2026');
      expect((await repo.signIn('D10001', 'Doctor@2026')).id, isNotEmpty);
      expect((await repo.signIn('D10001', DemoRepository.demoPassword)).id, isNotEmpty);
      await expectLater(repo.signIn('D10001', 'Other@2026'),
          throwsA(isA<ApiException>().having((e) => e.code, 'code', 'invalid_credentials')));
    });

    test('รหัสผ่านผิด → invalid_credentials', () {
      expect(() => repo.signIn('D10001', 'wrong'),
          throwsA(isA<ApiException>().having((e) => e.code, 'code', 'invalid_credentials')));
    });

    test('OTP ผิด → otp_invalid · เข้าครั้งแรกต้องยอมรับข้อกำหนด เปลี่ยนรหัสผ่าน และตั้ง PIN', () async {
      final c = await repo.signIn('d10001', DemoRepository.demoPassword);
      expect(c.maskedPhone, '081-XXX-5521');
      await expectLater(repo.verifySignInOtp(c.id, '000000'),
          throwsA(isA<ApiException>().having((e) => e.code, 'code', 'otp_invalid')));
      final r = await repo.verifySignInOtp(c.id, DemoRepository.demoOtp);
      expect(r.mustAcceptTerms && r.mustChangePassword && r.needsPin, isTrue);
      expect(r.session.isDoctor, isTrue);
      expect(r.session.hospitals, hasLength(3));
    });

    test('PIN ง่ายเกินไปถูกปฏิเสธ · ผิดเกิน 5 ครั้งล็อก', () async {
      await signIn('D10001');
      for (final weak in ['111111', '123456', '654321']) {
        await expectLater(repo.setPin(weak), throwsA(isA<ApiException>().having((e) => e.code, 'code', 'pin_too_simple')));
      }
      await repo.setPin('258036');
      expect((await repo.verifyPin('258036')).ok, isTrue);
      for (var i = 0; i < DemoRepository.maxPinAttempts - 1; i++) {
        final r = await repo.verifyPin('000001');
        expect(r.ok, isFalse);
        expect(r.attemptsLeft, DemoRepository.maxPinAttempts - i - 1);
      }
      await expectLater(repo.verifyPin('000001'), throwsA(isA<ApiException>().having((e) => e.code, 'code', 'pin_locked')));
      expect(await repo.restoreSession(), isNull);
    });

    test('ลืม PIN → PIN ชั่วคราวใช้ได้และต้องตั้งใหม่', () async {
      await signIn('D10001');
      await repo.setPin('258036');
      await repo.forgotPin();
      final r = await repo.verifyPin(DemoRepository.demoTempPin);
      expect(r.ok && r.temporary, isTrue);
    });

    test('ลงเวลานอกรัศมีไม่ได้ · ในรัศมีได้ · เช็คอินซ้ำไม่ได้', () async {
      await signIn('D10001');
      final site = await repo.attendanceSite();
      await expectLater(
        repo.recordAttendance(AttendanceType.checkIn, GeoPoint(site.location.lat + 0.02, site.location.lng)),
        throwsA(isA<ApiException>().having((e) => e.code, 'code', 'outside_area')),
      );
      final near = repo.demoDeviceLocation();
      expect(DemoRepository.distanceMeters(site.location, near), lessThan(site.radiusM));
      final e = await repo.recordAttendance(AttendanceType.checkIn, near);
      expect(e.inArea, isTrue);
      await expectLater(repo.recordAttendance(AttendanceType.checkIn, near),
          throwsA(isA<ApiException>().having((e) => e.code, 'code', 'already_checked_in')));
      await repo.recordAttendance(AttendanceType.checkOut, near);
      expect((await repo.attendanceToday()).isDone, isTrue);
    });

    test('อนุมัติทั้งชุด: ไม่อนุมัติต้องมีเหตุผล · มีรายการที่ตัดสินแล้วปนอยู่ = ไม่เปลี่ยนอะไรเลย', () async {
      await signIn('A20001');
      final pending = await repo.pendingApprovals();
      expect(pending, isNotEmpty);
      expect(pending.every((r) => r.status == ApprovalStatus.pending), isTrue);

      await expectLater(repo.decide([pending.first.id], Decision.reject),
          throwsA(isA<ApiException>().having((e) => e.code, 'code', 'comment_required')));

      await repo.decide([pending.first.id], Decision.approve);
      final before = (await repo.pendingApprovals()).length;
      await expectLater(repo.decide([pending.first.id, pending[1].id], Decision.approve),
          throwsA(isA<ApiException>().having((e) => e.code, 'code', 'already_decided')));
      expect((await repo.pendingApprovals()).length, before, reason: 'รายการที่สองต้องไม่ถูกอนุมัติไปครึ่งทาง');

      final d = await repo.approvalDetail(pending.first.id);
      expect(d.request.status, ApprovalStatus.approved);
      expect(d.steps.last.user, 'คุณศิริพร มั่นคง');
    });

    test('บัญชีแพทย์เห็นเฉพาะคำขอที่รอบทบาทตัวเอง', () async {
      await signIn('A20001');
      final types = (await repo.pendingApprovals()).map((r) => r.requestType).toSet();
      expect(types.contains(RequestType.doctorProfile), isFalse);
      expect(types.contains(RequestType.bankAccount), isTrue);
    });

    test('ใบแจ้งรายได้: ยอดรวมสุทธิ = 40(6) + 40(2) และเท่ากับยอดรายเดือน', () async {
      await signIn('D10001');
      final now = DateTime.now();
      final y = await repo.incomeYear(now.year);
      final m = y.months.first;
      final slip = await repo.paySlip(m.year, m.month);
      expect(slip.total, closeTo(m.net, 0.001));
      expect(slip.total, closeTo(slip.net406 + slip.net402, 0.001));
      expect(y.total, closeTo(y.months.fold(0.0, (a, e) => a + e.net), 0.001));
    });
  });
}

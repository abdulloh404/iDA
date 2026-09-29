
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:ida_mobile/app/app_controller.dart';
import 'package:ida_mobile/data/demo_repository.dart';
import 'package:ida_mobile/data/location_service.dart';
import 'package:ida_mobile/features/account/account_screen.dart';
import 'package:ida_mobile/features/attendance/attendance_screen.dart';
import 'package:ida_mobile/features/auth/login_screen.dart';
import 'package:ida_mobile/features/auth/unlock_screen.dart';
import 'package:ida_mobile/features/income/income_screen.dart';
import 'package:ida_mobile/features/requests/request_list_screen.dart';
import 'package:ida_mobile/features/shell/main_shell.dart';
import 'package:ida_mobile/main.dart';
import 'package:shared_preferences/shared_preferences.dart';

const _pin = '258036';

Future<AppController> _boot(WidgetTester tester, {Map<String, Object> prefs = const {}}) async {
  tester.view.physicalSize = const Size(1080, 2340);
  tester.view.devicePixelRatio = 3;
  addTearDown(tester.view.reset);
  SharedPreferences.setMockInitialValues(prefs);
  final p = await SharedPreferences.getInstance();
  final repo = DemoRepository(p, latency: Duration.zero);
  final app = AppController(
    repo: repo,
    location: DemoLocationService(repo.demoDeviceLocation, latency: Duration.zero),
    prefs: p,
    isDemo: true,
  );
  await tester.pumpWidget(IdaApp(controller: app));
  await app.boot();
  await tester.pumpAndSettle();
  return app;
}


Map<String, Object> _enrolled(String user) => {
      'demo.activated.$user': true,
      'demo.pin': _pin,
      'demo.pin.user': user,
    };


Future<void> _tap(WidgetTester tester, Finder f) async {
  await tester.ensureVisible(f);
  await tester.pumpAndSettle();
  await tester.tap(f);
}

Future<void> _enterPin(WidgetTester tester, String pin) async {
  for (final d in pin.split('')) {
    await tester.tap(find.text(d).last);
    await tester.pump();
  }
  await tester.pumpAndSettle();
}

void main() {
  testWidgets('เข้าใช้ครั้งแรก: รหัสผ่าน → OTP → ข้อกำหนด → รหัสผ่านใหม่ → PIN → หน้าหลัก', (tester) async {
    final app = await _boot(tester);
    expect(find.byType(LoginScreen), findsOneWidget);


    await tester.enterText(find.byKey(const Key('username')), 'D10001');
    await tester.enterText(find.byKey(const Key('password')), 'nope');
    await _tap(tester, find.byKey(const Key('sign-in')));
    await tester.pumpAndSettle();
    expect(find.text('รหัสผู้ใช้งานหรือรหัสผ่านไม่ถูกต้อง'), findsOneWidget);

    await tester.enterText(find.byKey(const Key('password')), DemoRepository.demoPassword);
    await _tap(tester, find.byKey(const Key('sign-in')));
    await tester.pumpAndSettle();
    expect(find.text('กรอกรหัส 6 หลักที่ส่งไปยัง 081-XXX-5521'), findsOneWidget);

    await tester.enterText(find.byKey(const Key('otp')), '999999');
    await tester.pumpAndSettle();
    expect(find.textContaining('รหัส OTP ไม่ถูกต้อง'), findsOneWidget);

    await tester.enterText(find.byKey(const Key('otp')), DemoRepository.demoOtp);
    await tester.pumpAndSettle();


    expect(find.byKey(const Key('terms-accept')), findsNothing);
    await tester.tap(find.byKey(const Key('terms-scroll')));
    await tester.pumpAndSettle();
    await tester.tap(find.byKey(const Key('terms-accept')));
    await tester.pumpAndSettle();


    final submit = find.byKey(const Key('password-submit'));
    await tester.enterText(find.byKey(const Key('new-password')), 'weakpass');
    await tester.pump();
    await _tap(tester, submit);
    await tester.pump();
    expect(find.text('ตั้งรหัส PIN'), findsNothing);
    await tester.enterText(find.byKey(const Key('new-password')), 'Doctor@2026');
    await tester.enterText(find.byKey(const Key('confirm-password')), 'Doctor@2026');
    await tester.pump();
    await _tap(tester, submit);
    await tester.pumpAndSettle();


    await _enterPin(tester, '123456');
    await _enterPin(tester, '123456');
    expect(find.textContaining('ง่ายเกินไป'), findsOneWidget);
    await _enterPin(tester, _pin);
    await _enterPin(tester, '000000');
    expect(find.text('รหัส PIN ไม่ตรงกัน กรุณาตั้งใหม่'), findsOneWidget);
    await _enterPin(tester, _pin);
    await _enterPin(tester, _pin);

    expect(app.stage, AuthStage.ready);
    expect(find.byType(MainShell), findsOneWidget);
    expect(find.text('นพ.ธนากร วัฒนศิริ'), findsWidgets);


    app.lock();
    await tester.pumpAndSettle();
    expect(find.byType(UnlockScreen), findsOneWidget);
    await _enterPin(tester, '111112');
    expect(find.textContaining('เหลืออีก 4 ครั้ง'), findsOneWidget);
    await _enterPin(tester, _pin);
    expect(find.byType(MainShell), findsOneWidget);
  });

  testWidgets('แพทย์: เช็คอินจากหน้าหลัก แล้วดูรายได้หลังยืนยัน PIN', (tester) async {
    final app = await _boot(tester, prefs: _enrolled('D10001'));
    await _enterPin(tester, _pin);
    expect(find.text('ยังไม่ได้ลงเวลาเข้างาน'), findsOneWidget);

    await _tap(tester, find.byKey(const Key('home-check')));
    await tester.pumpAndSettle();
    expect(find.byType(AttendanceScreen), findsOneWidget);
    expect(find.textContaining('อยู่ในพื้นที่'), findsOneWidget);

    await _tap(tester, find.byKey(const Key('check-in')));
    await tester.pumpAndSettle();
    await tester.tap(find.byKey(const Key('confirm-check')));
    await tester.pumpAndSettle();
    expect(find.textContaining('เช็คอินสำเร็จ'), findsOneWidget);
    expect((await app.repo.attendanceToday()).isWorking, isTrue);

    await tester.tap(find.byType(BackButton));
    await tester.pumpAndSettle();
    expect(find.textContaining('กำลังปฏิบัติงาน'), findsOneWidget);


    expect(app.amountsHidden, isTrue);
    await tester.drag(find.byType(ListView).first, const Offset(0, 1500));
    await tester.pumpAndSettle();
    await tester.tap(find.text('รายได้วันนี้'));
    await tester.pumpAndSettle();
    expect(find.text('ยืนยันตัวตนเพื่อดูรายได้'), findsOneWidget);
    await _enterPin(tester, _pin);
    expect(find.byType(IncomeScreen), findsOneWidget);
    expect(app.amountsHidden, isFalse);
  });

  testWidgets('บัญชีแพทย์: อนุมัติทีละรายการ และไม่อนุมัติหลายรายการพร้อมเหตุผล', (tester) async {
    final app = await _boot(tester, prefs: _enrolled('A20001'));
    await _enterPin(tester, _pin);
    final before = app.pendingApprovals;
    expect(before, greaterThan(2));

    await _tap(tester, find.byKey(const Key('review-now')));
    await tester.pumpAndSettle();
    expect(find.byType(RequestListScreen), findsOneWidget);


    await tester.tap(find.byTooltip('อนุมัติรายการนี้').first);
    await tester.pumpAndSettle();
    await tester.tap(find.widgetWithText(FilledButton, 'อนุมัติ'));
    await tester.pumpAndSettle();
    expect(find.text('อนุมัติแล้ว 1 รายการ'), findsOneWidget);
    expect(app.pendingApprovals, before - 1);


    await tester.tap(find.byKey(const Key('select-mode')));
    await tester.pumpAndSettle();
    final tiles = find.byWidgetPredicate((w) => w.key is ValueKey<String> && (w.key as ValueKey<String>).value.startsWith('req-'));
    await tester.tap(tiles.at(0));
    await tester.tap(tiles.at(1));
    await tester.pump();
    expect(find.text('เลือกแล้ว 2 รายการ'), findsOneWidget);

    await tester.tap(find.widgetWithText(OutlinedButton, 'ไม่อนุมัติ'));
    await tester.pumpAndSettle();
    await _tap(tester, find.byKey(const Key('reason-confirm')));
    await tester.pump();
    expect(find.text('กรุณาระบุเหตุผล'), findsOneWidget);
    await tester.enterText(find.byKey(const Key('reason')), 'เอกสารแนบไม่ครบ');
    await _tap(tester, find.byKey(const Key('reason-confirm')));
    await tester.pumpAndSettle();
    expect(find.text('ไม่อนุมัติ 2 รายการ'), findsOneWidget);
    expect(app.pendingApprovals, before - 3);
  });

  testWidgets('สลับภาษาเป็นอังกฤษจากหน้าบัญชี', (tester) async {
    await _boot(tester, prefs: _enrolled('D10001'));
    await _enterPin(tester, _pin);
    await tester.tap(find.text('บัญชี'));
    await tester.pumpAndSettle();
    expect(find.byType(AccountScreen), findsOneWidget);
    await _tap(tester, find.text('English'));
    await tester.pumpAndSettle();
    expect(find.text('My account'), findsOneWidget);
    expect(find.text('Sign out'), findsOneWidget);
  });
}

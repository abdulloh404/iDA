

import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:ida_mobile/app/app_controller.dart';
import 'package:ida_mobile/data/demo_repository.dart';
import 'package:ida_mobile/data/location_service.dart';
import 'package:ida_mobile/data/models.dart';
import 'package:ida_mobile/features/attendance/attendance_history_screen.dart';
import 'package:ida_mobile/features/attendance/attendance_screen.dart';
import 'package:ida_mobile/features/income/documents_screen.dart';
import 'package:ida_mobile/features/income/income_screen.dart';
import 'package:ida_mobile/features/income/payslip_screen.dart';
import 'package:ida_mobile/features/profile/bank_screen.dart';
import 'package:ida_mobile/features/profile/edit_request_screen.dart';
import 'package:ida_mobile/features/profile/profile_screen.dart';
import 'package:ida_mobile/features/requests/request_detail_screen.dart';
import 'package:ida_mobile/features/requests/request_list_screen.dart';
import 'package:ida_mobile/features/schedule/schedule_screen.dart';
import 'package:ida_mobile/features/shell/main_shell.dart';
import 'package:ida_mobile/main.dart';
import 'package:shared_preferences/shared_preferences.dart';

const _pin = '258036';

Future<AppController> _open(WidgetTester tester, String user, {required bool dark, String lang = 'th'}) async {
  tester.view.physicalSize = const Size(960, 2040);
  tester.view.devicePixelRatio = 3;
  tester.platformDispatcher.textScaleFactorTestValue = 1.3;
  addTearDown(tester.view.reset);
  addTearDown(tester.platformDispatcher.clearTextScaleFactorTestValue);
  SharedPreferences.setMockInitialValues({
    'demo.activated.$user': true,
    'demo.pin': _pin,
    'demo.pin.user': user,
    'ida.theme': dark ? 'dark' : 'light',
    'ida.locale': lang,
  });
  final prefs = await SharedPreferences.getInstance();
  final repo = DemoRepository(prefs, latency: Duration.zero);
  final app = AppController(
    repo: repo,
    location: DemoLocationService(repo.demoDeviceLocation, latency: Duration.zero),
    prefs: prefs,
    isDemo: true,
  );
  await tester.pumpWidget(IdaApp(controller: app));
  await app.boot();
  await tester.pumpAndSettle();

  for (final d in _pin.split('')) {
    await tester.tap(find.text(d).last);
    await tester.pump();
  }
  await tester.pumpAndSettle();
  expect(find.byType(MainShell), findsOneWidget);
  app.markIncomeUnlocked();
  return app;
}

Future<void> _visit(WidgetTester tester, AppController app, Widget page) async {
  app.navigatorKey.currentState!.push(MaterialPageRoute<void>(builder: (_) => page));
  await tester.pumpAndSettle();

  final scrollables = find.byType(Scrollable);
  if (scrollables.evaluate().isNotEmpty) {
    await tester.drag(scrollables.first, const Offset(0, -4000));
    await tester.pumpAndSettle();
  }
  app.navigatorKey.currentState!.pop();
  await tester.pumpAndSettle();
}

void main() {
  for (final dark in [false, true]) {
    final theme = dark ? 'มืด' : 'สว่าง';

    testWidgets('แพทย์ · ทุกหน้าจอ · 320px · ธีม$theme', (tester) async {
      final app = await _open(tester, 'D10001', dark: dark);
      final now = DateTime.now();
      final mine = await app.repo.myRequests();
      for (final page in <Widget>[
        const AttendanceScreen(),
        const AttendanceHistoryScreen(),
        const ScheduleScreen(),
        const IncomeScreen(),
        PaySlipScreen(year: now.year, month: now.month),
        const DocumentsScreen(),
        const ProfileScreen(),
        const BankScreen(),
        const EditProfileRequestScreen(),
        const EditBankRequestScreen(),
        const RequestListScreen(scope: RequestScope.mine),
        const RequestListScreen(scope: RequestScope.history),
        RequestDetailScreen(id: mine.firstWhere((r) => r.status == ApprovalStatus.rejected).id),
      ]) {
        await _visit(tester, app, page);
      }

      for (var i = 1; i <= 3; i++) {
        MainShell.goToTab(tester.element(find.byType(IndexedStack)), i);
        await tester.pumpAndSettle();
      }
    });

    testWidgets('เจ้าหน้าที่ · ทุกหน้าจอ · 320px · ธีม$theme · English', (tester) async {
      final app = await _open(tester, 'A20001', dark: dark, lang: 'en');
      final pending = await app.repo.pendingApprovals();
      for (final page in <Widget>[
        const RequestListScreen(scope: RequestScope.pending),
        const RequestListScreen(scope: RequestScope.history),
        RequestDetailScreen(id: pending.first.id),
      ]) {
        await _visit(tester, app, page);
      }
      for (var i = 1; i <= 3; i++) {
        MainShell.goToTab(tester.element(find.byType(IndexedStack)), i);
        await tester.pumpAndSettle();
      }
    });
  }
}

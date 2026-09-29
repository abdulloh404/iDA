import 'package:flutter/material.dart';

import '../../app/app_controller.dart';
import '../../l10n/strings.dart';
import '../../ui/components.dart';
import '../attendance/attendance_history_screen.dart';
import '../attendance/attendance_screen.dart';
import '../income/documents_screen.dart';
import '../income/income_gate.dart';
import '../income/income_screen.dart';
import '../profile/bank_screen.dart';
import '../profile/edit_request_screen.dart';
import '../profile/profile_screen.dart';
import '../requests/request_list_screen.dart';
import '../schedule/schedule_screen.dart';

enum MenuAudience { all, doctor, approver }


class AppMenu {
  const AppMenu(this.id, this.icon, this.label, this.open, {this.audience = MenuAudience.all});

  final String id;
  final IconData icon;
  final String Function(S s) label;
  final void Function(BuildContext context) open;
  final MenuAudience audience;

  bool visibleTo(AppController app) {
    final session = app.session;
    if (session == null) return false;
    return switch (audience) {
      MenuAudience.all => true,
      MenuAudience.doctor => session.isDoctor,
      MenuAudience.approver => session.canApprove,
    };
  }

  static final profile = AppMenu('profile', Icons.badge_outlined, (s) => s.menuProfile,
      (c) => pushPage<void>(c, const ProfileScreen()), audience: MenuAudience.doctor);
  static final bank = AppMenu('bank', Icons.account_balance_outlined, (s) => s.menuBank,
      (c) => pushPage<void>(c, const BankScreen()), audience: MenuAudience.doctor);
  static final editProfile = AppMenu('edit-profile', Icons.edit_note_rounded, (s) => s.menuEditProfile,
      (c) => pushPage<void>(c, const EditProfileRequestScreen()), audience: MenuAudience.doctor);
  static final editBank = AppMenu('edit-bank', Icons.price_change_outlined, (s) => s.menuEditBank,
      (c) => pushPage<void>(c, const EditBankRequestScreen()), audience: MenuAudience.doctor);
  static final checkIn = AppMenu('check-in', Icons.pin_drop_outlined, (s) => s.menuCheckIn,
      (c) => pushPage<void>(c, const AttendanceScreen()), audience: MenuAudience.doctor);
  static final attendanceHistory = AppMenu('attendance-history', Icons.history_rounded, (s) => s.menuAttendanceHistory,
      (c) => pushPage<void>(c, const AttendanceHistoryScreen()), audience: MenuAudience.doctor);
  static final schedule = AppMenu('schedule', Icons.calendar_month_outlined, (s) => s.menuSchedule,
      (c) => pushPage<void>(c, const ScheduleScreen()), audience: MenuAudience.doctor);
  static final income = AppMenu('income', Icons.payments_outlined, (s) => s.menuIncome,
      (c) => openIncome(c, () => const IncomeScreen()), audience: MenuAudience.doctor);
  static final documents = AppMenu('documents', Icons.receipt_long_outlined, (s) => s.menuDocuments,
      (c) => openIncome(c, () => const DocumentsScreen()), audience: MenuAudience.doctor);
  static final myRequests = AppMenu('my-requests', Icons.outbox_outlined, (s) => s.menuMyRequests,
      (c) => pushPage<void>(c, const RequestListScreen(scope: RequestScope.mine)));
  static final pending = AppMenu('pending', Icons.pending_actions_rounded, (s) => s.menuPending,
      (c) => pushPage<void>(c, const RequestListScreen(scope: RequestScope.pending)),
      audience: MenuAudience.approver);
  static final history = AppMenu('history', Icons.manage_history_rounded, (s) => s.menuHistory,
      (c) => pushPage<void>(c, const RequestListScreen(scope: RequestScope.history)));
}


class MenuTile extends StatelessWidget {
  const MenuTile({super.key, required this.icon, required this.label, required this.onTap, this.badge = 0});

  final IconData icon;
  final String label;
  final VoidCallback onTap;
  final int badge;

  @override
  Widget build(BuildContext context) {
    return Semantics(
      button: true,
      label: badge > 0 ? '$label ($badge)' : label,
      excludeSemantics: true,
      child: InkWell(
        borderRadius: IdaRadius.lgR,
        onTap: onTap,
        child: Padding(
          padding: const EdgeInsets.symmetric(vertical: IdaSpace.s2, horizontal: IdaSpace.s1),
          child: Column(mainAxisSize: MainAxisSize.min, children: [
            Badge(
              isLabelVisible: badge > 0,
              label: Text('$badge'),
              offset: const Offset(6, -6),
              child: IconBox(icon, size: IdaSizes.tileIcon + IdaSpace.s2),
            ),
            const SizedBox(height: IdaSpace.s2),
            Text(
              label,
              textAlign: TextAlign.center,
              maxLines: 2,
              overflow: TextOverflow.ellipsis,
              style: context.text.labelMedium!.copyWith(color: context.ida.text, fontWeight: FontWeight.w500),
            ),
          ]),
        ),
      ),
    );
  }
}


class MenuGrid extends StatelessWidget {
  const MenuGrid({super.key, required this.items});
  final List<Widget> items;

  @override
  Widget build(BuildContext context) => LayoutBuilder(builder: (context, c) {
        final cols = c.maxWidth > 480 ? 5 : 4;
        final w = c.maxWidth / cols;
        return Wrap(
          children: [for (final i in items) SizedBox(width: w, child: i)],
        );
      });
}

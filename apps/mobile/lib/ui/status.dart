import 'package:flutter/material.dart';

import '../data/models.dart';
import '../l10n/strings.dart';
import 'components.dart';


class StatusBadge extends StatelessWidget {
  const StatusBadge(this.status, {super.key});
  final ApprovalStatus status;

  @override
  Widget build(BuildContext context) {
    final s = S.of(context);
    final (tone, icon) = switch (status) {
      ApprovalStatus.pending => (IdaBadgeTone.pending, Icons.schedule_rounded),
      ApprovalStatus.approved => (IdaBadgeTone.success, Icons.check_circle_rounded),
      ApprovalStatus.rejected => (IdaBadgeTone.error, Icons.cancel_rounded),
      ApprovalStatus.returned => (IdaBadgeTone.pending, Icons.undo_rounded),
      ApprovalStatus.cancelled => (IdaBadgeTone.neutral, Icons.block_rounded),
      ApprovalStatus.draft => (IdaBadgeTone.info, Icons.edit_note_rounded),
    };
    return IdaBadge(label: s.status(status), tone: tone, icon: icon);
  }
}

IconData requestTypeIcon(String type) => switch (type) {
      RequestType.doctorProfile => Icons.badge_outlined,
      RequestType.bankAccount => Icons.account_balance_outlined,
      RequestType.doctorCode => Icons.qr_code_2_rounded,
      RequestType.specialty => Icons.workspace_premium_outlined,
      RequestType.contract => Icons.description_outlined,
      RequestType.welfare => Icons.volunteer_activism_outlined,
      _ => Icons.assignment_outlined,
    };

IconData notificationIcon(NotificationKind k) => switch (k) {
      NotificationKind.approval => Icons.fact_check_outlined,
      NotificationKind.income => Icons.payments_outlined,
      NotificationKind.schedule => Icons.event_available_outlined,
      NotificationKind.document => Icons.mark_email_read_outlined,
      NotificationKind.security => Icons.shield_outlined,
    };

IconData documentIcon(DocumentKind k) => switch (k) {
      DocumentKind.paySlip => Icons.receipt_long_outlined,
      DocumentKind.incomeCertificate => Icons.verified_outlined,
      DocumentKind.withholdingTax => Icons.account_balance_wallet_outlined,
    };


class DateBlock extends StatelessWidget {
  const DateBlock(this.date, {super.key, this.highlight = false});
  final DateTime date;
  final bool highlight;

  @override
  Widget build(BuildContext context) {
    final p = context.ida;
    final en = S.of(context).en;
    const weekdaysTh = ['จ.', 'อ.', 'พ.', 'พฤ.', 'ศ.', 'ส.', 'อา.'];
    const weekdaysEn = ['Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat', 'Sun'];
    return Container(
      width: IdaSizes.tileIcon + IdaSpace.s2,
      padding: const EdgeInsets.symmetric(vertical: IdaSpace.s2),
      decoration: BoxDecoration(
        color: highlight ? p.primary : p.primarySoft,
        borderRadius: IdaRadius.lgR,
      ),
      child: Column(mainAxisSize: MainAxisSize.min, children: [
        Text((en ? weekdaysEn : weekdaysTh)[date.weekday - 1],
            style: context.text.labelMedium!.copyWith(color: highlight ? p.onPrimary : p.primaryText)),
        Text('${date.day}',
            style: idaNumeric(context.text.titleMedium!)
                .copyWith(color: highlight ? p.onPrimary : p.primaryText, height: 1.2)),
      ]),
    );
  }
}

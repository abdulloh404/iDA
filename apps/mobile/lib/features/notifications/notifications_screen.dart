import 'package:flutter/material.dart';

import '../../app/app_controller.dart';
import '../../core/format.dart';
import '../../data/models.dart';
import '../../l10n/strings.dart';
import '../../ui/components.dart';
import '../../ui/status.dart';
import '../income/income_gate.dart';
import '../income/payslip_screen.dart';
import '../requests/request_detail_screen.dart';
import '../schedule/schedule_screen.dart';


class NotificationsScreen extends StatefulWidget {
  const NotificationsScreen({super.key, this.visit = 0});


  final int visit;

  @override
  State<NotificationsScreen> createState() => _NotificationsScreenState();
}

class _NotificationsScreenState extends State<NotificationsScreen> {
  final _view = GlobalKey<AsyncViewState<List<AppNotification>>>();

  Future<void> _open(AppNotification n) async {
    final app = AppScope.read(context);
    if (!n.read) {
      await app.repo.markNotificationRead(n.id);
      app.refreshBadges();
      _view.currentState?.reload();
    }
    if (!mounted) return;
    final target = n.targetId;
    switch (n.kind) {
      case NotificationKind.approval when target != null:
        await pushPage<void>(context, RequestDetailScreen(id: target));
      case NotificationKind.income when target != null:
        final parts = target.split('-');
        await openIncome(context, () => PaySlipScreen(year: int.parse(parts[0]), month: int.parse(parts[1])));
      case NotificationKind.schedule:
        await pushPage<void>(context, const ScheduleScreen());
      default:
        break;
    }
  }

  Future<void> _readAll() async {
    final app = AppScope.read(context);
    try {
      await app.repo.markAllNotificationsRead();
      await app.refreshBadges();
      await _view.currentState?.reload();
    } catch (e) {
      if (mounted) showErrorToast(context, e);
    }
  }

  @override
  Widget build(BuildContext context) {
    final s = S.of(context);
    final app = AppScope.of(context);
    return Scaffold(
      appBar: AppBar(
        title: Text(s.notificationsTitle),
        automaticallyImplyLeading: false,
        actions: [
          if (app.unreadNotifications > 0)
            TextButton(
              style: TextButton.styleFrom(foregroundColor: IdaColors.textInverse),
              onPressed: _readAll,
              child: Text(s.markAllRead),
            ),
        ],
      ),
      body: AsyncView<List<AppNotification>>(
        key: _view,
        reloadKey: '${widget.visit}-${app.dataVersion}',
        load: () => AppScope.read(context).repo.notifications(),
        builder: (context, list, reload) {
          final today = Fmt.dayOf(DateTime.now());
          final recent = list.where((n) => !Fmt.dayOf(n.at).isBefore(today.subtract(const Duration(days: 1)))).toList();
          final earlier = list.where((n) => !recent.contains(n)).toList();
          return RefreshIndicator(
            onRefresh: () async {
              await reload();
              await app.refreshBadges();
            },
            child: list.isEmpty
                ? ListView(children: [
                    EmptyState(
                      icon: Icons.notifications_none_rounded,
                      title: s.emptyNotifications,
                      message: s.emptyNotificationsBody,
                    ),
                  ])
                : ListView(
                    padding: const EdgeInsets.fromLTRB(IdaSpace.s4, IdaSpace.s4, IdaSpace.s4, IdaSpace.s8),
                    children: [
                      ContentWidth(
                        child: Column(crossAxisAlignment: CrossAxisAlignment.stretch, children: [
                          if (recent.isNotEmpty) ...[
                            SectionHeader('${s.today} / ${s.yesterday}'),
                            _Group(items: recent, onTap: _open),
                            const SizedBox(height: IdaSpace.s5),
                          ],
                          if (earlier.isNotEmpty) ...[
                            SectionHeader(s.earlier),
                            _Group(items: earlier, onTap: _open),
                          ],
                        ]),
                      ),
                    ],
                  ),
          );
        },
      ),
    );
  }
}

class _Group extends StatelessWidget {
  const _Group({required this.items, required this.onTap});
  final List<AppNotification> items;
  final ValueChanged<AppNotification> onTap;

  @override
  Widget build(BuildContext context) {
    final p = context.ida;
    return IdaCard(
      padding: EdgeInsets.zero,
      child: Column(children: [
        for (final (i, n) in items.indexed) ...[
          if (i > 0) Divider(indent: IdaSpace.s16, color: p.border),
          _Tile(n: n, onTap: () => onTap(n)),
        ],
      ]),
    );
  }
}

class _Tile extends StatelessWidget {
  const _Tile({required this.n, required this.onTap});
  final AppNotification n;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    final s = S.of(context);
    final p = context.ida;
    final unread = !n.read;
    return Semantics(
      label: unread ? s.unread : null,
      child: InkWell(
        key: Key('notif-${n.id}'),
        onTap: onTap,
        child: Container(
          color: unread ? p.primarySoft.withValues(alpha: context.isDark ? 0.5 : 0.45) : null,
          padding: const EdgeInsets.all(IdaSpace.s4),
          child: Row(crossAxisAlignment: CrossAxisAlignment.start, children: [
            IconBox(
              notificationIcon(n.kind),
              size: IdaSizes.avatarMd - IdaSpace.s1,
              bg: unread ? p.surface : p.surface2,
              fg: unread ? p.primaryText : p.textSecondary,
            ),
            const SizedBox(width: IdaSpace.s3),
            Expanded(
              child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                Row(children: [
                  Expanded(
                    child: Text(
                      s.en ? n.titleEn : n.titleTh,
                      style: context.text.titleSmall!.copyWith(fontWeight: unread ? FontWeight.w700 : FontWeight.w500),
                    ),
                  ),
                  if (unread) IdaBadge(label: s.newBadge, tone: IdaBadgeTone.info),
                ]),
                const SizedBox(height: 2),
                Text(s.en ? n.bodyEn : n.bodyTh, style: context.text.bodyMedium!.copyWith(color: p.textSecondary)),
                const SizedBox(height: IdaSpace.s1),
                Text(Fmt.relative(n.at, s.en), style: idaNumeric(context.text.bodySmall!)),
              ]),
            ),
          ]),
        ),
      ),
    );
  }
}

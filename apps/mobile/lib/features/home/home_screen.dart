import 'package:flutter/material.dart';
import 'package:flutter/services.dart';

import '../../app/app_controller.dart';
import '../../core/format.dart';
import '../../data/models.dart';
import '../../l10n/strings.dart';
import '../../ui/components.dart';
import '../attendance/attendance_screen.dart';
import '../income/income_gate.dart';
import '../income/income_screen.dart';
import '../requests/request_detail_screen.dart';
import '../requests/request_list_screen.dart';
import '../requests/request_tile.dart';
import '../schedule/schedule_screen.dart';
import '../schedule/shift_tile.dart';
import '../services/menu.dart';
import '../shell/hospital_switcher.dart';
import '../shell/main_shell.dart';


class HomeScreen extends StatelessWidget {
  const HomeScreen({super.key});

  @override
  Widget build(BuildContext context) {
    final app = AppScope.of(context);
    final session = app.requireSession;
    return AnnotatedRegion<SystemUiOverlayStyle>(
      value: SystemUiOverlayStyle.light,
      child: session.isDoctor
          ? _DoctorHome(key: ValueKey('d-${app.dataVersion}'))
          : _StaffHome(key: ValueKey('s-${app.dataVersion}')),
    );
  }
}


class _HomeHeader extends StatelessWidget {
  const _HomeHeader({required this.overlap});
  final Widget overlap;

  static const _overlapDepth = IdaSpace.s12 + IdaSpace.s4;

  @override
  Widget build(BuildContext context) {
    final app = AppScope.of(context);
    final s = S.of(context);
    final session = app.requireSession;
    final en = app.isEnglish;
    final hour = DateTime.now().hour;
    final greet = hour < 12 ? s.goodMorning : (hour < 17 ? s.goodAfternoon : s.goodEvening);
    final unread = app.unreadNotifications;
    const white = IdaColors.textInverse;

    return Stack(
      children: [
        const Positioned.fill(
          bottom: _overlapDepth,
          child: DecoratedBox(
            decoration: BoxDecoration(
              gradient: IdaColors.gradientCardAccent,
              borderRadius: BorderRadius.vertical(bottom: Radius.circular(IdaRadius.xxl + IdaSpace.s2)),
            ),
          ),
        ),

        Positioned(
          right: -IdaSpace.s8,
          top: MediaQuery.paddingOf(context).top + IdaSpace.s10,
          child: const ExcludeSemantics(
            child: Opacity(opacity: 0.05, child: IdaLogo.mark(height: 120, reversed: true)),
          ),
        ),
        Padding(
          padding: EdgeInsets.fromLTRB(IdaSpace.s4, MediaQuery.paddingOf(context).top + IdaSpace.s2, IdaSpace.s4, 0),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              Row(
                children: [
                  const Flexible(
                    child: Align(alignment: Alignment.centerLeft, child: HospitalPill()),
                  ),
                  const SizedBox(width: IdaSpace.s2),
                  IconButton(
                    tooltip: s.navNotifications,
                    onPressed: () => MainShell.goToTab(context, 2),
                    icon: Badge(
                      isLabelVisible: unread > 0,
                      label: Text('$unread'),
                      child: const Icon(Icons.notifications_none_rounded, color: white),
                    ),
                  ),
                ],
              ),
              const SizedBox(height: IdaSpace.s4),
              Row(
                children: [
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(greet, style: context.text.bodyMedium!.copyWith(color: white.withValues(alpha: 0.9))),
                        Text(
                          session.user.displayName(en),
                          maxLines: 1,
                          overflow: TextOverflow.ellipsis,
                          style: context.text.titleLarge!.copyWith(color: white, fontWeight: FontWeight.w700),
                        ),
                        Text(
                          '${session.user.position(en)} · ${session.hospital.roleName(en)}',
                          maxLines: 1,
                          overflow: TextOverflow.ellipsis,
                          style: context.text.bodySmall!.copyWith(color: white.withValues(alpha: 0.9)),
                        ),
                      ],
                    ),
                  ),
                  const SizedBox(width: IdaSpace.s3),
                  GestureDetector(
                    onTap: () => MainShell.goToTab(context, 3),
                    child: IdaAvatar(session.user.initials, size: IdaSizes.avatarMd + IdaSpace.s2, onBrand: true),
                  ),
                ],
              ),
              const SizedBox(height: IdaSpace.s5),
              overlap,
            ],
          ),
        ),
      ],
    );
  }
}


enum _Range { today, week, month }

class _DoctorData {
  const _DoctorData(this.dashboard, this.shifts, this.requests);
  final DoctorDashboard dashboard;
  final List<DutyShift> shifts;
  final List<ApprovalRequest> requests;
}

class _DoctorHome extends StatefulWidget {
  const _DoctorHome({super.key});

  @override
  State<_DoctorHome> createState() => _DoctorHomeState();
}

class _DoctorHomeState extends State<_DoctorHome> {
  _Range _range = _Range.week;

  Future<_DoctorData> _load() async {
    final repo = AppScope.read(context).repo;
    final today = Fmt.dayOf(DateTime.now());
    final endOfWeek = today.add(Duration(days: 7 - today.weekday));
    final endOfMonth = DateTime(today.year, today.month + 1, 0);
    final to = endOfWeek.isAfter(endOfMonth) ? endOfWeek : endOfMonth;
    final r = await Future.wait([repo.doctorDashboard(), repo.shifts(today, to), repo.myRequests()]);
    return _DoctorData(r[0] as DoctorDashboard, r[1] as List<DutyShift>, r[2] as List<ApprovalRequest>);
  }

  List<DutyShift> _filter(List<DutyShift> all) {
    final today = Fmt.dayOf(DateTime.now());
    final endOfWeek = today.add(Duration(days: 7 - today.weekday));
    return all.where((sh) {
      return switch (_range) {
        _Range.today => Fmt.sameDay(sh.date, today),
        _Range.week => !sh.date.isAfter(endOfWeek),
        _Range.month => sh.date.month == today.month,
      };
    }).toList();
  }

  @override
  Widget build(BuildContext context) {
    final s = S.of(context);
    final app = AppScope.of(context);
    return AsyncView<_DoctorData>(
      load: _load,
      loading: const _HomeSkeleton(),
      builder: (context, data, reload) {
        final shifts = _filter(data.shifts);
        final menu = [
          AppMenu.checkIn,
          AppMenu.schedule,
          AppMenu.income,
          AppMenu.documents,
          AppMenu.profile,
          AppMenu.bank,
          AppMenu.myRequests,
        ];
        return RefreshIndicator(
          onRefresh: () async {
            await reload();
            await app.refreshBadges();
          },
          edgeOffset: MediaQuery.paddingOf(context).top,
          child: ListView(
            padding: EdgeInsets.zero,
            children: [
              _HomeHeader(overlap: _IncomeCard(dashboard: data.dashboard)),
              ContentWidth(
                child: Padding(
                  padding: const EdgeInsets.fromLTRB(IdaSpace.s4, IdaSpace.s4, IdaSpace.s4, IdaSpace.s8),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.stretch,
                    children: [
                      _TodayCard(day: data.dashboard.today, shifts: data.dashboard.todayShifts, onChanged: reload),
                      const SizedBox(height: IdaSpace.s4),
                      IdaCard(
                        padding: const EdgeInsets.fromLTRB(IdaSpace.s2, IdaSpace.s4, IdaSpace.s2, IdaSpace.s2),
                        child: MenuGrid(
                          items: [
                            for (final m in menu)
                              MenuTile(icon: m.icon, label: m.label(s), onTap: () => m.open(context)),
                            MenuTile(
                              icon: Icons.apps_rounded,
                              label: s.seeAll,
                              onTap: () => MainShell.goToTab(context, 1),
                            ),
                          ],
                        ),
                      ),
                      const SizedBox(height: IdaSpace.s6),
                      SectionHeader(
                        s.menuSchedule,
                        action: s.seeAll,
                        onAction: () => pushPage<void>(context, const ScheduleScreen()),
                      ),
                      IdaSegmented<_Range>(
                        options: {_Range.today: s.today, _Range.week: s.thisWeek, _Range.month: s.thisMonth},
                        value: _range,
                        onChanged: (v) => setState(() => _range = v),
                      ),
                      const SizedBox(height: IdaSpace.s3),
                      if (shifts.isEmpty)
                        IdaCard(
                          child: EmptyState(icon: Icons.event_available_outlined, title: s.noShiftsRange),
                        )
                      else ...[
                        for (final sh in shifts.take(4)) ...[ShiftTile(sh), const SizedBox(height: IdaSpace.s2)],
                        if (shifts.length > 4)
                          Center(
                            child: TextButton(
                              onPressed: () => pushPage<void>(context, const ScheduleScreen()),
                              child: Text('${s.seeAll} (${s.shiftCount(shifts.length)})'),
                            ),
                          ),
                      ],
                      const SizedBox(height: IdaSpace.s6),
                      SectionHeader(
                        s.menuMyRequests,
                        action: s.seeAll,
                        onAction: () => pushPage<void>(context, const RequestListScreen(scope: RequestScope.mine)),
                      ),
                      if (data.requests.isEmpty)
                        IdaCard(
                          child: EmptyState(icon: Icons.outbox_outlined, title: s.emptyMine, message: s.emptyMineBody),
                        )
                      else
                        for (final r in data.requests.take(3)) ...[
                          RequestTile(
                            request: r,
                            onTap: () async {
                              await pushPage<void>(context, RequestDetailScreen(id: r.id));
                              reload();
                            },
                          ),
                          const SizedBox(height: IdaSpace.s2),
                        ],
                    ],
                  ),
                ),
              ),
            ],
          ),
        );
      },
    );
  }
}


class _IncomeCard extends StatelessWidget {
  const _IncomeCard({required this.dashboard});
  final DoctorDashboard dashboard;

  @override
  Widget build(BuildContext context) {
    final app = AppScope.of(context);
    final s = S.of(context);
    final p = context.ida;
    final hidden = app.amountsHidden;
    final d = dashboard;
    return IdaCard(
      padding: const EdgeInsets.fromLTRB(IdaSpace.s5, IdaSpace.s4, IdaSpace.s2, IdaSpace.s4),
      onTap: () => openIncome(context, () => const IncomeScreen()),
      semanticLabel: s.menuIncome,
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              Expanded(
                child: Wrap(
                  spacing: IdaSpace.s2,
                  runSpacing: IdaSpace.s1,
                  crossAxisAlignment: WrapCrossAlignment.center,
                  children: [
                    Text(s.todayIncome, style: context.text.labelLarge!.copyWith(color: p.textSecondary)),
                    IdaBadge(label: s.estimated, tone: IdaBadgeTone.neutral, icon: Icons.schedule_rounded),
                  ],
                ),
              ),
              IconButton(
                key: const Key('toggle-amounts'),
                tooltip: hidden ? s.showAmounts : s.hideAmounts,
                onPressed: app.toggleAmounts,
                icon: Icon(hidden ? Icons.visibility_outlined : Icons.visibility_off_outlined, size: IdaSizes.iconMd),
              ),
            ],
          ),
          IdaAmount(
            d.todayIncome,
            symbol: true,
            hidden: hidden,
            textAlign: TextAlign.left,
            style: context.text.headlineSmall!.copyWith(fontWeight: FontWeight.w700),
          ),
          const SizedBox(height: IdaSpace.s3),
          Divider(color: p.border),
          const SizedBox(height: IdaSpace.s3),
          Row(
            children: [
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(s.monthIncome, style: context.text.bodySmall),
                    IdaAmount(
                      d.monthIncome,
                      symbol: true,
                      hidden: hidden,
                      textAlign: TextAlign.left,
                      style: context.text.titleSmall,
                    ),
                  ],
                ),
              ),
              if (!hidden)
                IdaBadge(
                  label: Fmt.percent(d.monthLastYearDelta),
                  tone: d.monthLastYearDelta >= 0 ? IdaBadgeTone.success : IdaBadgeTone.error,
                  icon: d.monthLastYearDelta >= 0 ? Icons.trending_up_rounded : Icons.trending_down_rounded,
                ),
              Icon(Icons.chevron_right_rounded, color: p.borderStrong),
            ],
          ),
          const SizedBox(height: IdaSpace.s1),
          Text(s.asOf('${Fmt.date(d.asOf, s.en)} ${Fmt.time(d.asOf)}'), style: context.text.bodySmall),
        ],
      ),
    );
  }
}


class _TodayCard extends StatelessWidget {
  const _TodayCard({required this.day, required this.shifts, required this.onChanged});

  final AttendanceDay day;
  final List<DutyShift> shifts;
  final Future<void> Function() onChanged;

  @override
  Widget build(BuildContext context) {
    final s = S.of(context);
    final p = context.ida;
    final (status, icon, bg, fg) = day.isDone
        ? (s.checkedOutAt(Fmt.time(day.checkOut!.at)), Icons.task_alt_rounded, p.successSoft, p.success)
        : day.isWorking
        ? (s.workingSince(Fmt.time(day.checkIn!.at)), Icons.timelapse_rounded, p.accentSoft, p.accentStrong)
        : (s.notCheckedIn, Icons.schedule_rounded, p.warningSoft, p.warningText);
    final onDuty = shifts.where((x) => x.status == ShiftStatus.onDuty).toList();

    return IdaCard(
      padding: const EdgeInsets.all(IdaSpace.s4),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Row(
            children: [
              IconBox(icon, size: IdaSizes.avatarMd, bg: bg, fg: fg),
              const SizedBox(width: IdaSpace.s3),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(s.todayWork, style: context.text.bodySmall),
                    Text(status, style: context.text.titleSmall),
                  ],
                ),
              ),
            ],
          ),
          const SizedBox(height: IdaSpace.s3),
          if (onDuty.isEmpty)
            Text(s.noShiftToday, style: context.text.bodyMedium!.copyWith(color: p.textSecondary))
          else
            for (final sh in onDuty)
              Padding(
                padding: const EdgeInsets.only(bottom: IdaSpace.s1),
                child: Row(
                  children: [
                    Icon(Icons.medical_services_outlined, size: IdaSizes.iconSm, color: p.textSecondary),
                    const SizedBox(width: IdaSpace.s2),
                    Expanded(
                      child: Text(
                        '${sh.clinic(s.en)} · ${sh.room}',
                        maxLines: 1,
                        overflow: TextOverflow.ellipsis,
                        style: context.text.bodyMedium,
                      ),
                    ),
                    Text(
                      '${Fmt.minuteOfDay(sh.startMinute)}–${Fmt.minuteOfDay(sh.endMinute)}',
                      style: idaNumeric(context.text.bodyMedium!).copyWith(color: p.textSecondary),
                    ),
                  ],
                ),
              ),
          if (!day.isDone) ...[
            const SizedBox(height: IdaSpace.s3),
            PrimaryButton(
              key: const Key('home-check'),
              label: day.isWorking ? s.checkOut : s.menuCheckIn,
              icon: day.isWorking ? Icons.logout_rounded : Icons.login_rounded,
              onPressed: () async {
                await pushPage<void>(context, const AttendanceScreen());
                onChanged();
              },
            ),
          ],
        ],
      ),
    );
  }
}


class _StaffData {
  const _StaffData(this.pending, this.mine);
  final List<ApprovalRequest> pending;
  final List<ApprovalRequest> mine;
}

class _StaffHome extends StatelessWidget {
  const _StaffHome({super.key});

  @override
  Widget build(BuildContext context) {
    final s = S.of(context);
    final app = AppScope.of(context);
    final repo = app.repo;
    return AsyncView<_StaffData>(
      load: () async {
        final r = await Future.wait([repo.pendingApprovals(), repo.myRequests()]);
        return _StaffData(r[0], r[1]);
      },
      loading: const _HomeSkeleton(),
      builder: (context, data, reload) {
        Future<void> openPending() async {
          await pushPage<void>(context, const RequestListScreen(scope: RequestScope.pending));
          await reload();
          await app.refreshBadges();
        }

        Future<void> openDetail(ApprovalRequest r) async {
          await pushPage<void>(context, RequestDetailScreen(id: r.id));
          await reload();
          await app.refreshBadges();
        }

        return RefreshIndicator(
          onRefresh: () async {
            await reload();
            await app.refreshBadges();
          },
          edgeOffset: MediaQuery.paddingOf(context).top,
          child: ListView(
            padding: EdgeInsets.zero,
            children: [
              _HomeHeader(
                overlap: _PendingCard(count: data.pending.length, onOpen: openPending),
              ),
              ContentWidth(
                child: Padding(
                  padding: const EdgeInsets.fromLTRB(IdaSpace.s4, IdaSpace.s4, IdaSpace.s4, IdaSpace.s8),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.stretch,
                    children: [
                      IdaCard(
                        padding: const EdgeInsets.fromLTRB(IdaSpace.s2, IdaSpace.s4, IdaSpace.s2, IdaSpace.s2),
                        child: MenuGrid(
                          items: [
                            if (app.requireSession.canApprove)
                              MenuTile(
                                icon: AppMenu.pending.icon,
                                label: AppMenu.pending.label(s),
                                badge: data.pending.length,
                                onTap: openPending,
                              ),
                            MenuTile(
                              icon: AppMenu.myRequests.icon,
                              label: AppMenu.myRequests.label(s),
                              onTap: () => AppMenu.myRequests.open(context),
                            ),
                            MenuTile(
                              icon: AppMenu.history.icon,
                              label: AppMenu.history.label(s),
                              onTap: () => AppMenu.history.open(context),
                            ),
                            MenuTile(
                              icon: Icons.person_outline_rounded,
                              label: s.menuMyInfo,
                              onTap: () => MainShell.goToTab(context, 3),
                            ),
                          ],
                        ),
                      ),
                      const SizedBox(height: IdaSpace.s6),
                      SectionHeader(
                        s.menuPending,
                        action: data.pending.isEmpty ? null : s.seeAll,
                        onAction: openPending,
                      ),
                      if (data.pending.isEmpty)
                        IdaCard(
                          child: EmptyState(
                            icon: Icons.task_alt_rounded,
                            title: s.allCaughtUp,
                            message: s.allCaughtUpBody,
                          ),
                        )
                      else
                        for (final r in data.pending.take(4)) ...[
                          RequestTile(request: r, showRequester: true, onTap: () => openDetail(r)),
                          const SizedBox(height: IdaSpace.s2),
                        ],
                      const SizedBox(height: IdaSpace.s6),
                      SectionHeader(
                        s.menuMyRequests,
                        action: data.mine.isEmpty ? null : s.seeAll,
                        onAction: () => AppMenu.myRequests.open(context),
                      ),
                      if (data.mine.isEmpty)
                        IdaCard(
                          child: EmptyState(icon: Icons.outbox_outlined, title: s.emptyMine),
                        )
                      else
                        for (final r in data.mine.take(3)) ...[
                          RequestTile(request: r, onTap: () => openDetail(r)),
                          const SizedBox(height: IdaSpace.s2),
                        ],
                    ],
                  ),
                ),
              ),
            ],
          ),
        );
      },
    );
  }
}

class _PendingCard extends StatelessWidget {
  const _PendingCard({required this.count, required this.onOpen});
  final int count;
  final VoidCallback onOpen;

  @override
  Widget build(BuildContext context) {
    final s = S.of(context);
    final p = context.ida;
    final empty = count == 0;
    return IdaCard(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Row(
            children: [
              IconBox(
                empty ? Icons.task_alt_rounded : Icons.pending_actions_rounded,
                size: IdaSizes.avatarLg - IdaSpace.s2,
                bg: empty ? p.successSoft : p.warningSoft,
                fg: empty ? p.success : p.warningText,
              ),
              const SizedBox(width: IdaSpace.s4),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(s.pendingForYou, style: context.text.bodySmall),
                    Text(
                      empty ? s.allCaughtUp : s.pendingCount(count),
                      style: (empty ? context.text.titleMedium : idaNumeric(context.text.headlineSmall!))!,
                    ),
                  ],
                ),
              ),
            ],
          ),

          if (!empty) ...[
            const SizedBox(height: IdaSpace.s4),
            PrimaryButton(
              key: const Key('review-now'),
              label: s.reviewNow,
              icon: Icons.arrow_forward_rounded,
              onPressed: onOpen,
            ),
          ],
        ],
      ),
    );
  }
}

class _HomeSkeleton extends StatelessWidget {
  const _HomeSkeleton();

  @override
  Widget build(BuildContext context) => ListView(
    physics: const NeverScrollableScrollPhysics(),
    padding: EdgeInsets.zero,
    children: [
      Container(
        height: MediaQuery.paddingOf(context).top + 200,
        decoration: const BoxDecoration(
          gradient: IdaColors.gradientCardAccent,
          borderRadius: BorderRadius.vertical(bottom: Radius.circular(IdaRadius.xxl + IdaSpace.s2)),
        ),
      ),
      const Padding(
        padding: IdaSizes.screenPadding,
        child: Column(
          children: [
            Skeleton(height: 120, radius: IdaRadius.xlR),
            SizedBox(height: IdaSpace.s4),
            Skeleton(height: 180, radius: IdaRadius.xlR),
            SizedBox(height: IdaSpace.s4),
            Skeleton(height: 80, radius: IdaRadius.xlR),
          ],
        ),
      ),
    ],
  );
}

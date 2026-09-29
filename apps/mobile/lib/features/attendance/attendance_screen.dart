import 'dart:async';
import 'dart:math' as math;

import 'package:flutter/material.dart';

import '../../app/app_controller.dart';
import '../../core/format.dart';
import '../../data/demo_repository.dart';
import '../../data/location_service.dart';
import '../../data/models.dart';
import '../../l10n/strings.dart';
import '../../ui/components.dart';
import 'attendance_history_screen.dart';


class AttendanceScreen extends StatefulWidget {
  const AttendanceScreen({super.key});

  @override
  State<AttendanceScreen> createState() => _AttendanceScreenState();
}

enum _Confirm { ok, refresh }

class _AttendanceScreenState extends State<AttendanceScreen> {
  AttendanceSite? _site;
  AttendanceDay? _day;
  Object? _loadError;
  LocationResult? _location;
  bool _locating = false;
  bool _saving = false;
  late Timer _clock;
  DateTime _now = DateTime.now();

  @override
  void initState() {
    super.initState();
    _clock = Timer.periodic(const Duration(seconds: 1), (_) {
      if (mounted) setState(() => _now = DateTime.now());
    });
    _load();
  }

  @override
  void dispose() {
    _clock.cancel();
    super.dispose();
  }

  Future<void> _load() async {
    final repo = AppScope.read(context).repo;
    setState(() => _loadError = null);
    try {
      final r = await Future.wait([repo.attendanceSite(), repo.attendanceToday()]);
      if (!mounted) return;
      setState(() {
        _site = r[0] as AttendanceSite;
        _day = r[1] as AttendanceDay;
      });
      await _locate();
    } catch (e) {
      if (mounted) setState(() => _loadError = e);
    }
  }

  Future<void> _locate() async {
    setState(() => _locating = true);
    final r = await AppScope.read(context).location.current();
    if (mounted) {
      setState(() {
        _location = r;
        _locating = false;
      });
    }
  }

  double? get _distance {
    final loc = _location, site = _site;
    if (loc is! LocationOk || site == null) return null;
    return DemoRepository.distanceMeters(site.location, loc.point);
  }

  bool get _inArea => (_distance ?? double.infinity) <= (_site?.radiusM ?? 0);
  bool get _canRecord => _distance != null && (_inArea || (_site?.allowOutside ?? false));

  Future<void> _record(AttendanceType type) async {
    final s = S.of(context);
    final isIn = type == AttendanceType.checkIn;
    final choice = await showModalBottomSheet<_Confirm>(
      context: context,
      isScrollControlled: true,
      builder: (ctx) => _ConfirmSheet(
        isIn: isIn,
        site: _site!,
        distance: _distance!,
        inArea: _inArea,
      ),
    );
    if (!mounted || choice == null) return;
    if (choice == _Confirm.refresh) {
      await _locate();
      return;
    }
    final loc = _location;
    if (loc is! LocationOk) return;
    final repo = AppScope.read(context).repo;
    setState(() => _saving = true);
    try {
      final event = await repo.recordAttendance(type, loc.point);
      final day = await repo.attendanceToday();
      if (!mounted) return;
      setState(() => _day = day);
      showToast(context, isIn ? s.checkedInToast(Fmt.time(event.at)) : s.checkedOutToast(Fmt.time(event.at)));
    } catch (e) {
      if (mounted) showErrorToast(context, e);
    } finally {
      if (mounted) setState(() => _saving = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final s = S.of(context);
    final p = context.ida;
    final en = s.en;
    return Scaffold(
      appBar: AppBar(
        title: Text(s.menuCheckIn),
        actions: [
          IconButton(
            tooltip: s.menuAttendanceHistory,
            icon: const Icon(Icons.history_rounded),
            onPressed: () => pushPage<void>(context, const AttendanceHistoryScreen()),
          ),
        ],
      ),
      body: _loadError != null
          ? ErrorState(error: _loadError!, onRetry: _load)
          : _site == null
              ? const SkeletonList(header: true, count: 2)
              : RefreshIndicator(
                  onRefresh: _load,
                  child: ListView(
                    padding: const EdgeInsets.fromLTRB(IdaSpace.s4, IdaSpace.s5, IdaSpace.s4, IdaSpace.s8),
                    children: [
                      ContentWidth(
                        child: Column(crossAxisAlignment: CrossAxisAlignment.stretch, children: [
                          Text(Fmt.dateFull(_now, en),
                              textAlign: TextAlign.center,
                              style: context.text.bodyLarge!.copyWith(color: p.textSecondary)),
                          Text(
                            Fmt.timeWithSeconds(_now),
                            textAlign: TextAlign.center,
                            style: idaNumeric(context.text.headlineSmall!).copyWith(fontWeight: FontWeight.w600),
                          ),
                          const SizedBox(height: IdaSpace.s5),
                          Center(child: _WorkRing(day: _day!, now: _now)),
                          const SizedBox(height: IdaSpace.s5),
                          _LocationCard(
                            site: _site!,
                            result: _location,
                            distance: _distance,
                            inArea: _inArea,
                            locating: _locating,
                            onRefresh: _locate,
                          ),
                          const SizedBox(height: IdaSpace.s4),
                          Row(children: [
                            Expanded(
                              child: PrimaryButton(
                                key: const Key('check-in'),
                                label: s.checkIn,
                                icon: Icons.login_rounded,
                                loading: _saving && _day!.checkIn == null,
                                onPressed: _day!.checkIn == null && _canRecord && !_locating && !_saving
                                    ? () => _record(AttendanceType.checkIn)
                                    : null,
                              ),
                            ),
                            const SizedBox(width: IdaSpace.s3),
                            Expanded(
                              child: SizedBox(
                                height: IdaSizes.controlHeightLg,
                                child: OutlinedButton.icon(
                                  key: const Key('check-out'),
                                  onPressed: _day!.isWorking && _canRecord && !_locating && !_saving
                                      ? () => _record(AttendanceType.checkOut)
                                      : null,
                                  icon: const Icon(Icons.logout_rounded, size: IdaSizes.iconMd),
                                  label: Text(s.checkOut),
                                ),
                              ),
                            ),
                          ]),
                          const SizedBox(height: IdaSpace.s6),
                          if (_day!.checkIn != null) ...[
                            SectionHeader(s.todayLog),
                            IdaCard(
                              padding: const EdgeInsets.symmetric(horizontal: IdaSpace.s4, vertical: IdaSpace.s2),
                              child: Column(children: [
                                _EventRow(event: _day!.checkIn!),
                                if (_day!.checkOut != null) ...[
                                  Divider(color: p.border),
                                  _EventRow(event: _day!.checkOut!),
                                ],
                              ]),
                            ),
                          ],
                        ]),
                      ),
                    ],
                  ),
                ),
    );
  }
}


class _WorkRing extends StatelessWidget {
  const _WorkRing({required this.day, required this.now});
  final AttendanceDay day;
  final DateTime now;

  static const _size = 208.0;
  static const _fullDay = Duration(hours: 8);

  @override
  Widget build(BuildContext context) {
    final s = S.of(context);
    final p = context.ida;
    final worked = day.worked(now);
    final progress = (worked.inSeconds / _fullDay.inSeconds).clamp(0.0, 1.0);
    final (label, tone, icon) = day.isDone
        ? (s.statusDone, IdaBadgeTone.success, Icons.task_alt_rounded)
        : day.isWorking
            ? (s.statusWorking, IdaBadgeTone.info, Icons.timelapse_rounded)
            : (s.statusNotStarted, IdaBadgeTone.pending, Icons.schedule_rounded);
    return Semantics(
      label: '${s.workHours} ${Fmt.durationWords(worked, s.en)} · $label',
      excludeSemantics: true,
      child: SizedBox.square(
        dimension: _size,
        child: CustomPaint(
          painter: _RingPainter(
            progress: progress,
            track: p.skeleton,
            color: day.isDone ? p.success : p.primary,
          ),
          child: Center(
            child: Column(mainAxisSize: MainAxisSize.min, children: [
              Text(Fmt.hoursMinutes(worked), style: idaNumeric(context.text.displaySmall!)),
              Text(s.workHours, style: context.text.bodySmall),
              const SizedBox(height: IdaSpace.s2),
              IdaBadge(label: label, tone: tone, icon: icon),
            ]),
          ),
        ),
      ),
    );
  }
}

class _RingPainter extends CustomPainter {
  _RingPainter({required this.progress, required this.track, required this.color});
  final double progress;
  final Color track;
  final Color color;

  @override
  void paint(Canvas canvas, Size size) {
    const stroke = 14.0;
    final rect = Offset.zero & size;
    final r = rect.deflate(stroke / 2);
    final base = Paint()
      ..style = PaintingStyle.stroke
      ..strokeWidth = stroke
      ..strokeCap = StrokeCap.round;
    canvas.drawArc(r, 0, math.pi * 2, false, base..color = track);
    if (progress > 0) {
      canvas.drawArc(r, -math.pi / 2, math.pi * 2 * progress, false, base..color = color);
    }
  }

  @override
  bool shouldRepaint(_RingPainter old) => old.progress != progress || old.color != color || old.track != track;
}

class _LocationCard extends StatelessWidget {
  const _LocationCard({
    required this.site,
    required this.result,
    required this.distance,
    required this.inArea,
    required this.locating,
    required this.onRefresh,
  });

  final AttendanceSite site;
  final LocationResult? result;
  final double? distance;
  final bool inArea;
  final bool locating;
  final VoidCallback onRefresh;

  @override
  Widget build(BuildContext context) {
    final s = S.of(context);
    final p = context.ida;
    final en = s.en;
    final radius = Fmt.distance(site.radiusM, en);
    final location = AppScope.read(context).location;

    Widget? status;
    Widget? action;
    switch (result) {
      case LocationOk(:final point):
        status = Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
          Text(s.distanceFrom(Fmt.distance(distance!, en)), style: context.text.bodyMedium),
          const SizedBox(height: IdaSpace.s1),
          Wrap(spacing: IdaSpace.s2, runSpacing: IdaSpace.s1, crossAxisAlignment: WrapCrossAlignment.center, children: [
            inArea
                ? IdaBadge(label: s.inArea(radius), tone: IdaBadgeTone.success, icon: Icons.verified_rounded)
                : IdaBadge(
                    label: s.outArea(radius),
                    tone: site.allowOutside ? IdaBadgeTone.pending : IdaBadgeTone.error,
                    icon: Icons.wrong_location_rounded),
            if (point.accuracyM != null)
              Text(s.accuracy(Fmt.distance(point.accuracyM!, en)), style: context.text.bodySmall),
          ]),
          if (!inArea) ...[
            const SizedBox(height: IdaSpace.s2),
            Text(site.allowOutside ? s.outAreaAllowed : s.outAreaBlocked,
                style: context.text.bodySmall!.copyWith(color: site.allowOutside ? p.warningText : p.dangerText)),
          ],
        ]);
      case LocationDenied():
        status = Text(s.locationDenied, style: context.text.bodyMedium!.copyWith(color: p.dangerText));
        action = OutlinedButton(onPressed: onRefresh, child: Text(s.allowLocation));
      case LocationDeniedForever():
        status = Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
          Text(s.locationDenied, style: context.text.bodyMedium!.copyWith(color: p.dangerText)),
          Text(s.locationDeniedBody, style: context.text.bodySmall),
        ]);
        action = OutlinedButton(onPressed: location.openSettings, child: Text(s.openSettings));
      case LocationServiceOff():
        status = Text(s.locationOff, style: context.text.bodyMedium!.copyWith(color: p.dangerText));
        action = OutlinedButton(onPressed: location.openSettings, child: Text(s.openSettings));
      case LocationFailed():
        status = Text(s.errorNetwork, style: context.text.bodyMedium!.copyWith(color: p.dangerText));
      case null:
        status = null;
    }

    return IdaCard(
      padding: const EdgeInsets.fromLTRB(IdaSpace.s4, IdaSpace.s4, IdaSpace.s2, IdaSpace.s4),
      child: Row(crossAxisAlignment: CrossAxisAlignment.start, children: [
        IconBox(Icons.location_on_outlined, size: IdaSizes.avatarMd),
        const SizedBox(width: IdaSpace.s3),
        Expanded(
          child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
            Text(site.name(en), style: context.text.titleSmall),
            const SizedBox(height: 2),
            if (locating)
              Row(children: [
                SizedBox.square(
                    dimension: IdaSizes.iconSm, child: CircularProgressIndicator(strokeWidth: 2, color: p.accent)),
                const SizedBox(width: IdaSpace.s2),
                Text(s.locating, style: context.text.bodyMedium!.copyWith(color: p.textSecondary)),
              ])
            else ?status,
            if (action != null && !locating) ...[const SizedBox(height: IdaSpace.s2), action],
          ]),
        ),
        IconButton(
          key: const Key('refresh-location'),
          tooltip: s.refreshLocation,
          onPressed: locating ? null : onRefresh,
          icon: const Icon(Icons.my_location_rounded),
        ),
      ]),
    );
  }
}

class _EventRow extends StatelessWidget {
  const _EventRow({required this.event});
  final AttendanceEvent event;

  @override
  Widget build(BuildContext context) {
    final s = S.of(context);
    final p = context.ida;
    final isIn = event.type == AttendanceType.checkIn;
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: IdaSpace.s2),
      child: Row(children: [
        IconBox(isIn ? Icons.login_rounded : Icons.logout_rounded,
            size: IdaSizes.avatarSm,
            bg: isIn ? p.successSoft : p.primarySoft,
            fg: isIn ? p.success : p.primaryText),
        const SizedBox(width: IdaSpace.s3),
        Expanded(
          child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
            Text(isIn ? s.checkIn : s.checkOut, style: context.text.titleSmall),
            Text('${event.siteNameTh} · ${Fmt.distance(event.distanceM, s.en)}', style: context.text.bodySmall),
          ]),
        ),
        Column(crossAxisAlignment: CrossAxisAlignment.end, children: [
          Text(Fmt.time(event.at), style: idaNumeric(context.text.titleSmall!)),
          if (!event.inArea) IdaBadge(label: s.flaggedOutside, tone: IdaBadgeTone.pending, icon: Icons.flag_rounded),
        ]),
      ]),
    );
  }
}

class _ConfirmSheet extends StatelessWidget {
  const _ConfirmSheet({required this.isIn, required this.site, required this.distance, required this.inArea});

  final bool isIn;
  final AttendanceSite site;
  final double distance;
  final bool inArea;

  @override
  Widget build(BuildContext context) {
    final s = S.of(context);
    final p = context.ida;
    final now = DateTime.now();
    return SafeArea(
      child: SingleChildScrollView(
        padding: const EdgeInsets.fromLTRB(IdaSpace.s5, 0, IdaSpace.s5, IdaSpace.s4),
        child: Column(mainAxisSize: MainAxisSize.min, crossAxisAlignment: CrossAxisAlignment.stretch, children: [
          Center(
            child: IconBox(isIn ? Icons.login_rounded : Icons.logout_rounded,
                size: IdaSizes.avatarLg - IdaSpace.s4,
                bg: isIn ? p.successSoft : p.primarySoft,
                fg: isIn ? p.success : p.primaryText),
          ),
          const SizedBox(height: IdaSpace.s3),
          Text(s.confirmCheckTitle(isIn), textAlign: TextAlign.center, style: context.text.titleMedium),
          const SizedBox(height: IdaSpace.s1),
          Text(s.confirmCheckBody(isIn, site.name(s.en)),
              textAlign: TextAlign.center, style: context.text.bodyMedium!.copyWith(color: p.textSecondary)),
          const SizedBox(height: IdaSpace.s4),
          Container(
            padding: const EdgeInsets.all(IdaSpace.s4),
            decoration: BoxDecoration(color: p.surface2, borderRadius: IdaRadius.lgR),
            child: Row(children: [
              Expanded(
                child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                  Text(s.timeLabel, style: context.text.bodySmall),
                  Text(Fmt.time(now), style: idaNumeric(context.text.titleMedium!)),
                ]),
              ),
              Expanded(
                child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                  Text(s.distanceLabel, style: context.text.bodySmall),
                  Text(Fmt.distance(distance, s.en), style: idaNumeric(context.text.titleMedium!)),
                ]),
              ),
              inArea
                  ? IdaBadge(label: s.inAreaShort, tone: IdaBadgeTone.success, icon: Icons.verified_rounded)
                  : IdaBadge(label: s.flaggedOutside, tone: IdaBadgeTone.pending, icon: Icons.flag_rounded),
            ]),
          ),
          const SizedBox(height: IdaSpace.s5),
          PrimaryButton(
            key: const Key('confirm-check'),
            label: s.confirm,
            icon: Icons.check_rounded,
            onPressed: () => Navigator.pop(context, _Confirm.ok),
          ),
          const SizedBox(height: IdaSpace.s2),
          OutlinedButton.icon(
            onPressed: () => Navigator.pop(context, _Confirm.refresh),
            icon: const Icon(Icons.my_location_rounded, size: IdaSizes.iconMd),
            label: Text(s.refreshLocation),
          ),
          TextButton(onPressed: () => Navigator.pop(context), child: Text(s.cancel)),
        ]),
      ),
    );
  }
}

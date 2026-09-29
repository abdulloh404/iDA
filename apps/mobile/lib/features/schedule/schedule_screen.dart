import 'package:flutter/material.dart';

import '../../app/app_controller.dart';
import '../../core/format.dart';
import '../../data/models.dart';
import '../../l10n/strings.dart';
import '../../ui/components.dart';
import '../../ui/month_switcher.dart';
import 'shift_tile.dart';


class ScheduleScreen extends StatefulWidget {
  const ScheduleScreen({super.key});

  @override
  State<ScheduleScreen> createState() => _ScheduleScreenState();
}

enum _View { day, month }

class _ScheduleScreenState extends State<ScheduleScreen> {
  DateTime _month = DateTime(DateTime.now().year, DateTime.now().month);
  DateTime _selected = Fmt.dayOf(DateTime.now());
  _View _view = _View.day;

  void _changeMonth(DateTime m) => setState(() {
        _month = m;
        final today = DateTime.now();
        _selected = m.year == today.year && m.month == today.month ? Fmt.dayOf(today) : m;
      });

  @override
  Widget build(BuildContext context) {
    final s = S.of(context);
    final p = context.ida;
    return Scaffold(
      appBar: AppBar(title: Text(s.menuSchedule)),
      body: AsyncView<List<DutyShift>>(
        reloadKey: '$_month-${AppScope.of(context).dataVersion}',
        load: () => AppScope.read(context).repo.shifts(_month, DateTime(_month.year, _month.month + 1, 0)),
        builder: (context, shifts, reload) {
          final byDay = <DateTime, List<DutyShift>>{};
          for (final sh in shifts) {
            (byDay[Fmt.dayOf(sh.date)] ??= []).add(sh);
          }
          final list = _view == _View.day ? (byDay[_selected] ?? const <DutyShift>[]) : shifts;
          return RefreshIndicator(
            onRefresh: reload,
            child: ListView(
              padding: const EdgeInsets.only(bottom: IdaSpace.s8),
              children: [
                Container(
                  color: p.surface,
                  padding: const EdgeInsets.fromLTRB(IdaSpace.s2, IdaSpace.s1, IdaSpace.s2, IdaSpace.s4),
                  child: ContentWidth(
                    child: Column(children: [
                      MonthSwitcher(month: _month, onChanged: _changeMonth),
                      _Calendar(
                        month: _month,
                        selected: _selected,
                        byDay: byDay,
                        onSelect: (d) => setState(() {
                          _selected = d;
                          _view = _View.day;
                        }),
                      ),
                      const SizedBox(height: IdaSpace.s3),
                      const _Legend(),
                    ]),
                  ),
                ),
                Padding(
                  padding: const EdgeInsets.fromLTRB(IdaSpace.s4, IdaSpace.s4, IdaSpace.s4, 0),
                  child: ContentWidth(
                    child: Column(crossAxisAlignment: CrossAxisAlignment.stretch, children: [
                      IdaSegmented<_View>(
                        options: {
                          _View.day: Fmt.date(_selected, s.en),
                          _View.month: '${s.thisMonth} · ${s.shiftCount(shifts.length)}',
                        },
                        value: _view,
                        onChanged: (v) => setState(() => _view = v),
                      ),
                      const SizedBox(height: IdaSpace.s3),
                      if (list.isEmpty)
                        EmptyState(
                          icon: Icons.event_available_outlined,
                          title: _view == _View.day ? s.noShiftsDay : s.noShiftsRange,
                          message: _view == _View.day ? s.noShiftsDayBody : null,
                        )
                      else
                        for (final sh in list) ...[ShiftTile(sh), const SizedBox(height: IdaSpace.s2)],
                    ]),
                  ),
                ),
              ],
            ),
          );
        },
      ),
    );
  }
}

class _Calendar extends StatelessWidget {
  const _Calendar({required this.month, required this.selected, required this.byDay, required this.onSelect});

  final DateTime month;
  final DateTime selected;
  final Map<DateTime, List<DutyShift>> byDay;
  final ValueChanged<DateTime> onSelect;

  @override
  Widget build(BuildContext context) {
    final s = S.of(context);
    final p = context.ida;
    final first = DateTime(month.year, month.month);
    final days = DateTime(month.year, month.month + 1, 0).day;

    final lead = first.weekday % 7;
    final cells = lead + days;
    final rows = (cells / 7).ceil();
    final today = Fmt.dayOf(DateTime.now());
    const order = [7, 1, 2, 3, 4, 5, 6];

    return Column(children: [
      Row(children: [
        for (final wd in order)
          Expanded(
            child: Padding(
              padding: const EdgeInsets.symmetric(vertical: IdaSpace.s2),
              child: Text(
                Fmt.weekdayShort(wd, s.en),
                textAlign: TextAlign.center,
                style: context.text.labelMedium!.copyWith(color: wd >= 6 ? p.dangerText : p.textSecondary),
              ),
            ),
          ),
      ]),
      for (var r = 0; r < rows; r++)
        Row(children: [
          for (var c = 0; c < 7; c++)
            Expanded(
              child: Builder(builder: (context) {
                final n = r * 7 + c - lead + 1;
                if (n < 1 || n > days) return const SizedBox(height: IdaSizes.minTapTarget + IdaSpace.s3);
                final d = DateTime(month.year, month.month, n);
                final list = byDay[d] ?? const <DutyShift>[];
                return _DayCell(
                  day: d,
                  selected: Fmt.sameDay(d, selected),
                  today: Fmt.sameDay(d, today),
                  onDuty: list.any((x) => x.status == ShiftStatus.onDuty),
                  cancelled: list.any((x) => x.status == ShiftStatus.cancelled),
                  onTap: () => onSelect(d),
                );
              }),
            ),
        ]),
    ]);
  }
}

class _DayCell extends StatelessWidget {
  const _DayCell({
    required this.day,
    required this.selected,
    required this.today,
    required this.onDuty,
    required this.cancelled,
    required this.onTap,
  });

  final DateTime day;
  final bool selected;
  final bool today;
  final bool onDuty;
  final bool cancelled;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    final s = S.of(context);
    final p = context.ida;
    final label = [
      Fmt.dateLong(day, s.en),
      if (onDuty) s.onDuty,
      if (cancelled) s.cancelledDuty,
    ].join(', ');
    return Semantics(
      button: true,
      selected: selected,
      label: label,
      excludeSemantics: true,
      child: InkWell(
        borderRadius: IdaRadius.lgR,
        onTap: onTap,
        child: SizedBox(
          height: IdaSizes.minTapTarget + IdaSpace.s3,
          child: Column(mainAxisAlignment: MainAxisAlignment.center, children: [
            AnimatedContainer(
              duration: IdaMotion.of(context, IdaMotion.fast),
              width: IdaSpace.s8,
              height: IdaSpace.s8,
              alignment: Alignment.center,
              decoration: BoxDecoration(
                shape: BoxShape.circle,
                color: selected ? p.primary : Colors.transparent,
                border: today && !selected ? Border.all(color: p.primaryText, width: 1.5) : null,
              ),
              child: Text(
                '${day.day}',
                style: idaNumeric(context.text.bodyMedium!).copyWith(
                  color: selected ? p.onPrimary : (today ? p.primaryText : p.text),
                  fontWeight: selected || today ? FontWeight.w700 : FontWeight.w400,
                ),
              ),
            ),
            const SizedBox(height: 3),
            Row(mainAxisAlignment: MainAxisAlignment.center, children: [
              if (onDuty) const _Marker(onDuty: true),
              if (onDuty && cancelled) const SizedBox(width: 3),
              if (cancelled) const _Marker(onDuty: false),
              if (!onDuty && !cancelled) const SizedBox(height: _Marker.size),
            ]),
          ]),
        ),
      ),
    );
  }
}


class _Marker extends StatelessWidget {
  const _Marker({required this.onDuty});
  final bool onDuty;

  static const size = 7.0;

  @override
  Widget build(BuildContext context) {
    final p = context.ida;
    return Container(
      width: size,
      height: size,
      decoration: BoxDecoration(
        shape: BoxShape.circle,
        color: onDuty ? p.success : Colors.transparent,
        border: onDuty ? null : Border.all(color: p.danger, width: 1.6),
      ),
    );
  }
}

class _Legend extends StatelessWidget {
  const _Legend();

  @override
  Widget build(BuildContext context) {
    final s = S.of(context);
    Widget item(bool onDuty, String label) => Row(mainAxisSize: MainAxisSize.min, children: [
          _Marker(onDuty: onDuty),
          const SizedBox(width: IdaSpace.s1),
          Text(label, style: context.text.bodySmall),
        ]);
    return Wrap(spacing: IdaSpace.s5, children: [item(true, s.onDuty), item(false, s.cancelledDuty)]);
  }
}

import 'package:flutter/material.dart';

import '../../app/app_controller.dart';
import '../../core/format.dart';
import '../../data/models.dart';
import '../../l10n/strings.dart';
import '../../ui/components.dart';
import '../../ui/month_switcher.dart';
import '../../ui/status.dart';


class AttendanceHistoryScreen extends StatefulWidget {
  const AttendanceHistoryScreen({super.key});

  @override
  State<AttendanceHistoryScreen> createState() => _AttendanceHistoryScreenState();
}

class _AttendanceHistoryScreenState extends State<AttendanceHistoryScreen> {
  DateTime _month = DateTime(DateTime.now().year, DateTime.now().month);

  @override
  Widget build(BuildContext context) {
    final s = S.of(context);
    final p = context.ida;
    return Scaffold(
      appBar: AppBar(title: Text(s.menuAttendanceHistory)),
      body: Column(children: [
        Container(
          color: p.surface,
          padding: const EdgeInsets.symmetric(horizontal: IdaSpace.s2, vertical: IdaSpace.s1),
          child: MonthSwitcher(month: _month, max: DateTime.now(), onChanged: (m) => setState(() => _month = m)),
        ),
        Expanded(
          child: AsyncView<List<AttendanceDay>>(
            reloadKey: _month,
            load: () => AppScope.read(context).repo.attendanceHistory(_month.year, _month.month),
            builder: (context, days, reload) {
              final total = days.fold(Duration.zero, (a, d) => a + (d.isDone ? d.worked() : Duration.zero));
              return RefreshIndicator(
                onRefresh: reload,
                child: ListView(
                  padding: const EdgeInsets.fromLTRB(IdaSpace.s4, IdaSpace.s4, IdaSpace.s4, IdaSpace.s8),
                  children: [
                    ContentWidth(
                      child: Column(crossAxisAlignment: CrossAxisAlignment.stretch, children: [
                        IdaCard(
                          child: Row(children: [
                            Expanded(child: _Stat(label: s.daysLabel, value: s.daysWorked(days.length))),
                            Container(width: 1, height: IdaSpace.s10, color: p.border),
                            Expanded(child: _Stat(label: s.totalHours, value: Fmt.durationWords(total, s.en))),
                          ]),
                        ),
                        const SizedBox(height: IdaSpace.s4),
                        if (days.isEmpty)
                          EmptyState(
                            icon: Icons.event_note_outlined,
                            title: s.attendanceEmpty,
                            message: s.attendanceEmptyBody,
                          )
                        else
                          for (final d in days) ...[_DayRow(day: d), const SizedBox(height: IdaSpace.s2)],
                      ]),
                    ),
                  ],
                ),
              );
            },
          ),
        ),
      ]),
    );
  }
}

class _Stat extends StatelessWidget {
  const _Stat({required this.label, required this.value});
  final String label;
  final String value;

  @override
  Widget build(BuildContext context) => Column(children: [
        Text(label, style: context.text.bodySmall),
        const SizedBox(height: 2),
        Text(value, style: idaNumeric(context.text.titleMedium!)),
      ]);
}

class _DayRow extends StatelessWidget {
  const _DayRow({required this.day});
  final AttendanceDay day;

  @override
  Widget build(BuildContext context) {
    final s = S.of(context);
    final p = context.ida;
    final flagged = !(day.checkIn?.inArea ?? true) || !(day.checkOut?.inArea ?? true);
    Widget time(String label, AttendanceEvent? e) => Expanded(
          child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
            Text(label, style: context.text.bodySmall),
            Text(e == null ? '--:--' : Fmt.time(e.at), style: idaNumeric(context.text.titleSmall!)),
          ]),
        );
    return IdaCard(
      padding: const EdgeInsets.all(IdaSpace.s4),
      child: Row(children: [
        DateBlock(day.date, highlight: Fmt.sameDay(day.date, DateTime.now())),
        const SizedBox(width: IdaSpace.s4),
        Expanded(
          child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
            Row(children: [
              time(s.timeIn, day.checkIn),
              time(s.timeOut, day.checkOut),
              Column(crossAxisAlignment: CrossAxisAlignment.end, children: [
                Text(s.workHours, style: context.text.bodySmall),
                Text(day.isDone ? Fmt.hoursMinutes(day.worked()) : '-', style: idaNumeric(context.text.titleSmall!)),
              ]),
            ]),
            if (flagged || !day.isDone) ...[
              const SizedBox(height: IdaSpace.s2),
              Wrap(spacing: IdaSpace.s2, children: [
                if (flagged) IdaBadge(label: s.flaggedOutside, tone: IdaBadgeTone.pending, icon: Icons.flag_rounded),
                if (!day.isDone)
                  IdaBadge(label: s.stillWorking, tone: IdaBadgeTone.neutral, icon: Icons.more_time_rounded),
              ]),
            ],
            if (day.checkIn != null) ...[
              const SizedBox(height: IdaSpace.s1),
              Text(day.checkIn!.siteNameTh, style: context.text.bodySmall!.copyWith(color: p.textSecondary)),
            ],
          ]),
        ),
      ]),
    );
  }
}

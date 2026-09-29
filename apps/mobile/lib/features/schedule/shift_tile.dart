import 'package:flutter/material.dart';

import '../../core/format.dart';
import '../../data/models.dart';
import '../../l10n/strings.dart';
import '../../ui/components.dart';
import '../../ui/status.dart';


class ShiftStatusBadge extends StatelessWidget {
  const ShiftStatusBadge(this.status, {super.key});
  final ShiftStatus status;

  @override
  Widget build(BuildContext context) {
    final s = S.of(context);
    return status == ShiftStatus.onDuty
        ? IdaBadge(label: s.onDuty, tone: IdaBadgeTone.success, icon: Icons.event_available_rounded)
        : IdaBadge(label: s.cancelledDuty, tone: IdaBadgeTone.error, icon: Icons.event_busy_rounded);
  }
}


class ShiftTile extends StatelessWidget {
  const ShiftTile(this.shift, {super.key, this.showDate = true});

  final DutyShift shift;
  final bool showDate;

  @override
  Widget build(BuildContext context) {
    final s = S.of(context);
    final p = context.ida;
    final cancelled = shift.status == ShiftStatus.cancelled;
    final isToday = Fmt.sameDay(shift.date, DateTime.now());
    return IdaCard(
      padding: const EdgeInsets.all(IdaSpace.s4),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          if (showDate) ...[DateBlock(shift.date, highlight: isToday), const SizedBox(width: IdaSpace.s3)],
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  '${shift.clinic(s.en)} (${shift.room})',
                  style: context.text.titleSmall!.copyWith(
                    decoration: cancelled ? TextDecoration.lineThrough : null,
                    decorationColor: p.textSecondary,
                    color: cancelled ? p.textSecondary : p.text,
                  ),
                ),
                const SizedBox(height: 2),
                Row(
                  children: [
                    Icon(Icons.schedule_rounded, size: IdaSizes.iconXs, color: p.textSecondary),
                    const SizedBox(width: IdaSpace.s1),
                    Flexible(
                      child: Text(
                        '${Fmt.minuteOfDay(shift.startMinute)} – ${Fmt.minuteOfDay(shift.endMinute)}',
                        style: idaNumeric(context.text.bodyMedium!).copyWith(color: p.textSecondary),
                      ),
                    ),
                  ],
                ),
                Row(
                  children: [
                    Icon(Icons.local_hospital_outlined, size: IdaSizes.iconXs, color: p.textSecondary),
                    const SizedBox(width: IdaSpace.s1),
                    Flexible(
                      child: Text(
                        shift.hospitalNameTh,
                        maxLines: 1,
                        overflow: TextOverflow.ellipsis,
                        style: context.text.bodySmall,
                      ),
                    ),
                  ],
                ),
                const SizedBox(height: IdaSpace.s2),

                Wrap(
                  spacing: IdaSpace.s2,
                  runSpacing: IdaSpace.s1,
                  crossAxisAlignment: WrapCrossAlignment.center,
                  children: [
                    ShiftStatusBadge(shift.status),
                    if (shift.note != null)
                      Text(shift.note!, style: context.text.bodySmall!.copyWith(color: p.dangerText)),
                  ],
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }
}

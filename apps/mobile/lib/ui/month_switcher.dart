import 'package:flutter/material.dart';

import '../core/format.dart';
import '../l10n/strings.dart';
import 'components.dart';


class MonthSwitcher extends StatelessWidget {
  const MonthSwitcher({super.key, required this.month, required this.onChanged, this.max});


  final DateTime month;
  final ValueChanged<DateTime> onChanged;


  final DateTime? max;

  @override
  Widget build(BuildContext context) {
    final s = S.of(context);
    final canNext = max == null || DateTime(month.year, month.month + 1).isBefore(DateTime(max!.year, max!.month + 1));
    return Row(children: [
      IconButton(
        tooltip: s.prevMonth,
        onPressed: () => onChanged(DateTime(month.year, month.month - 1)),
        icon: const Icon(Icons.chevron_left_rounded),
      ),
      Expanded(
        child: Semantics(
          liveRegion: true,
          child: Text(
            Fmt.monthYear(month.year, month.month, s.en),
            textAlign: TextAlign.center,
            style: context.text.titleMedium,
          ),
        ),
      ),
      IconButton(
        tooltip: s.nextMonth,
        onPressed: canNext ? () => onChanged(DateTime(month.year, month.month + 1)) : null,
        icon: const Icon(Icons.chevron_right_rounded),
      ),
    ]);
  }
}

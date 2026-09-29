import 'package:flutter/material.dart';

import '../../app/app_controller.dart';
import '../../core/format.dart';
import '../../data/models.dart';
import '../../l10n/strings.dart';
import '../../ui/components.dart';
import '../../ui/month_switcher.dart';
import '../../ui/status.dart';
import 'documents_screen.dart';
import 'payslip_screen.dart';


class IncomeScreen extends StatefulWidget {
  const IncomeScreen({super.key});

  @override
  State<IncomeScreen> createState() => _IncomeScreenState();
}

enum _Tab { monthly, daily }

class _IncomeScreenState extends State<IncomeScreen> {
  int _year = DateTime.now().year;
  _Tab _tab = _Tab.monthly;
  DateTime _dailyMonth = DateTime(DateTime.now().year, DateTime.now().month);

  Future<(List<int>, IncomeYear)> _load() async {
    final repo = AppScope.read(context).repo;
    final r = await Future.wait([repo.incomeYears(), repo.incomeYear(_year)]);
    return (r[0] as List<int>, r[1] as IncomeYear);
  }

  void _openSlip(MonthlyIncome m) => pushPage<void>(context, PaySlipScreen(year: m.year, month: m.month));

  @override
  Widget build(BuildContext context) {
    final s = S.of(context);
    return Scaffold(
      appBar: AppBar(
        title: Text(s.menuIncome),
        actions: [
          IconButton(
            tooltip: s.menuDocuments,
            icon: const Icon(Icons.receipt_long_outlined),
            onPressed: () => pushPage<void>(context, const DocumentsScreen()),
          ),
        ],
      ),
      body: AsyncView<(List<int>, IncomeYear)>(
        reloadKey: '$_year-${AppScope.of(context).dataVersion}',
        load: _load,
        loading: const SkeletonList(header: true),
        builder: (context, data, reload) {
          final (years, year) = data;
          return RefreshIndicator(
            onRefresh: reload,
            child: ListView(
              padding: const EdgeInsets.fromLTRB(IdaSpace.s4, IdaSpace.s4, IdaSpace.s4, IdaSpace.s8),
              children: [
                ContentWidth(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.stretch,
                    children: [
                      _YearCard(
                        data: year,
                        years: years,
                        onYear: (y) => setState(() {
                          _year = y;
                          _dailyMonth = DateTime(y, y == DateTime.now().year ? DateTime.now().month : 12);
                        }),
                      ),
                      const SizedBox(height: IdaSpace.s4),
                      if (year.months.isNotEmpty) _MonthlyChart(data: year, onTap: _openSlip),
                      const SizedBox(height: IdaSpace.s5),
                      IdaSegmented<_Tab>(
                        options: {_Tab.monthly: s.monthly, _Tab.daily: s.daily},
                        value: _tab,
                        onChanged: (t) => setState(() => _tab = t),
                      ),
                      const SizedBox(height: IdaSpace.s3),
                      if (_tab == _Tab.monthly)
                        IdaCard(
                          padding: const EdgeInsets.symmetric(vertical: IdaSpace.s2),
                          child: Column(
                            children: [
                              for (final (i, m) in year.months.indexed) ...[
                                if (i > 0)
                                  Divider(indent: IdaSpace.s4, endIndent: IdaSpace.s4, color: context.ida.border),
                                _MonthRow(m: m, onTap: () => _openSlip(m)),
                              ],
                            ],
                          ),
                        )
                      else
                        _DailyList(month: _dailyMonth, year: _year, onMonth: (m) => setState(() => _dailyMonth = m)),
                    ],
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


class _YearCard extends StatelessWidget {
  const _YearCard({required this.data, required this.years, required this.onYear});

  final IncomeYear data;
  final List<int> years;
  final ValueChanged<int> onYear;

  @override
  Widget build(BuildContext context) {
    final s = S.of(context);
    final app = AppScope.of(context);
    final hidden = app.amountsHidden;
    const white = IdaColors.textInverse;
    return Container(
      padding: const EdgeInsets.fromLTRB(IdaSpace.s5, IdaSpace.s4, IdaSpace.s3, IdaSpace.s5),
      decoration: BoxDecoration(
        gradient: IdaColors.gradientCardAccent,
        borderRadius: IdaRadius.xlR,
        boxShadow: context.ida.shadowMd,
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              Expanded(
                child: Text(
                  s.yearTotal(Fmt.year(data.year, s.en).toString()),
                  style: context.text.bodyMedium!.copyWith(color: white.withValues(alpha: 0.92)),
                ),
              ),
              Material(
                color: white.withValues(alpha: 0.16),
                shape: const StadiumBorder(),
                child: InkWell(
                  key: const Key('pick-year'),
                  customBorder: const StadiumBorder(),
                  onTap: () async {
                    final y = await pickOption<int>(
                      context,
                      title: s.year,
                      selected: data.year,
                      options: [for (final y in years) (y, '${Fmt.year(y, s.en)}')],
                    );
                    if (y != null) onYear(y);
                  },
                  child: Padding(
                    padding: const EdgeInsets.symmetric(horizontal: IdaSpace.s3, vertical: IdaSpace.s2),
                    child: Row(
                      mainAxisSize: MainAxisSize.min,
                      children: [
                        Text('${Fmt.year(data.year, s.en)}', style: context.text.labelLarge!.copyWith(color: white)),
                        const Icon(Icons.expand_more_rounded, size: IdaSizes.iconMd, color: white),
                      ],
                    ),
                  ),
                ),
              ),
              IconButton(
                tooltip: hidden ? s.showAmounts : s.hideAmounts,
                onPressed: app.toggleAmounts,
                icon: Icon(
                  hidden ? Icons.visibility_outlined : Icons.visibility_off_outlined,
                  size: IdaSizes.iconMd,
                  color: white,
                ),
              ),
            ],
          ),
          const SizedBox(height: IdaSpace.s1),
          IdaAmount(
            data.total,
            symbol: true,
            hidden: hidden,
            textAlign: TextAlign.left,
            style: context.text.displaySmall!.copyWith(color: white),
          ),
          const SizedBox(height: IdaSpace.s3),
          Wrap(
            spacing: IdaSpace.s4,
            runSpacing: IdaSpace.s1,
            children: [
              _OnBrandMeta(icon: Icons.badge_outlined, text: '${s.doctorCode} ${data.doctorCode}'),
              _OnBrandMeta(icon: Icons.update_rounded, text: s.asOf(Fmt.date(data.asOf, s.en))),
            ],
          ),
        ],
      ),
    );
  }
}

class _OnBrandMeta extends StatelessWidget {
  const _OnBrandMeta({required this.icon, required this.text});
  final IconData icon;
  final String text;

  @override
  Widget build(BuildContext context) {
    const white = IdaColors.textInverse;
    return Row(
      mainAxisSize: MainAxisSize.min,
      children: [
        Icon(icon, size: IdaSizes.iconSm, color: white),
        const SizedBox(width: IdaSpace.s1),
        Text(text, style: idaNumeric(context.text.bodySmall!).copyWith(color: white)),
      ],
    );
  }
}


class _MonthlyChart extends StatelessWidget {
  const _MonthlyChart({required this.data, required this.onTap});
  final IncomeYear data;
  final ValueChanged<MonthlyIncome> onTap;

  static const _height = 150.0;

  @override
  Widget build(BuildContext context) {
    final s = S.of(context);
    final p = context.ida;
    final hidden = AppScope.of(context).amountsHidden;
    final months = [...data.months]..sort((a, b) => a.month.compareTo(b.month));
    final max = months.fold<double>(0, (m, e) => e.net > m ? e.net : m);
    final latest = months.last;
    return IdaCard(
      padding: const EdgeInsets.fromLTRB(IdaSpace.s4, IdaSpace.s4, IdaSpace.s4, IdaSpace.s3),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(s.chartMonthly, style: context.text.titleSmall),
          Text(s.chartHint, style: context.text.bodySmall),
          const SizedBox(height: IdaSpace.s4),
          SizedBox(
            height: _height + IdaSpace.s6,
            child: Row(
              crossAxisAlignment: CrossAxisAlignment.end,
              children: [
                for (var m = 1; m <= 12; m++)
                  Expanded(
                    child: Builder(
                      builder: (context) {
                        final item = months.where((e) => e.month == m).firstOrNull;
                        final h = item == null || max == 0 ? 0.0 : (item.net / max) * _height;
                        final isLatest = item != null && item.month == latest.month;
                        return Semantics(
                          button: item != null,
                          label: item == null
                              ? Fmt.month(m, s.en)
                              : '${Fmt.monthYear(data.year, m, s.en)} ${hidden ? '' : Fmt.money(item.net, symbol: true)}',
                          excludeSemantics: true,
                          child: GestureDetector(
                            onTap: item == null ? null : () => onTap(item),
                            behavior: HitTestBehavior.opaque,
                            child: Column(
                              mainAxisAlignment: MainAxisAlignment.end,
                              children: [
                                if (isLatest && !hidden)
                                  FittedBox(
                                    child: Text(
                                      Fmt.compact(item.net),
                                      style: idaNumeric(context.text.labelSmall!).copyWith(color: p.text),
                                    ),
                                  ),
                                const SizedBox(height: 2),
                                AnimatedContainer(
                                  duration: IdaMotion.of(context, IdaMotion.slow),
                                  curve: IdaMotion.ease,
                                  height: h,
                                  margin: const EdgeInsets.symmetric(horizontal: 3),
                                  decoration: BoxDecoration(

                                    color: p.chart[0].withValues(alpha: isLatest ? 1 : 0.55),
                                    borderRadius: const BorderRadius.vertical(top: Radius.circular(IdaRadius.sm)),
                                  ),
                                ),
                                const SizedBox(height: IdaSpace.s1),
                                FittedBox(
                                  child: Text(
                                    Fmt.monthShort(m, s.en),
                                    style: context.text.labelSmall!.copyWith(
                                      color: isLatest ? p.text : p.textSecondary,
                                      fontWeight: isLatest ? FontWeight.w700 : FontWeight.w500,
                                    ),
                                  ),
                                ),
                              ],
                            ),
                          ),
                        );
                      },
                    ),
                  ),
              ],
            ),
          ),
        ],
      ),
    );
  }
}

class _MonthRow extends StatelessWidget {
  const _MonthRow({required this.m, required this.onTap});
  final MonthlyIncome m;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    final s = S.of(context);
    final p = context.ida;
    final hidden = AppScope.of(context).amountsHidden;
    return InkWell(
      onTap: onTap,
      child: Padding(
        padding: const EdgeInsets.fromLTRB(IdaSpace.s4, IdaSpace.s3, IdaSpace.s2, IdaSpace.s3),
        child: Row(
          children: [
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(Fmt.month(m.month, s.en), style: context.text.titleSmall),
                  const SizedBox(height: 2),
                  m.paid
                      ? IdaBadge(
                          label: s.paidOn(Fmt.date(m.paidOn!, s.en)),
                          tone: IdaBadgeTone.success,
                          icon: Icons.check_circle_rounded,
                        )
                      : IdaBadge(label: s.awaitingPay, tone: IdaBadgeTone.pending, icon: Icons.schedule_rounded),
                ],
              ),
            ),
            IdaAmount(m.net, symbol: true, hidden: hidden, style: context.text.titleSmall),
            Icon(Icons.chevron_right_rounded, color: p.borderStrong),
          ],
        ),
      ),
    );
  }
}

class _DailyList extends StatelessWidget {
  const _DailyList({required this.month, required this.year, required this.onMonth});
  final DateTime month;
  final int year;
  final ValueChanged<DateTime> onMonth;

  @override
  Widget build(BuildContext context) {
    final s = S.of(context);
    final p = context.ida;
    final hidden = AppScope.of(context).amountsHidden;
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        MonthSwitcher(month: month, max: DateTime.now(), onChanged: onMonth),
        Container(
          padding: const EdgeInsets.all(IdaSpace.s3),
          decoration: BoxDecoration(color: p.accentSoft, borderRadius: IdaRadius.mdR),
          child: Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Icon(Icons.info_outline_rounded, size: IdaSizes.iconSm, color: p.accentStrong),
              const SizedBox(width: IdaSpace.s2),
              Expanded(
                child: Text(s.dailyNote, style: context.text.bodySmall!.copyWith(color: p.accentStrong)),
              ),
            ],
          ),
        ),
        const SizedBox(height: IdaSpace.s3),
        AsyncView<List<DailyIncome>>(
          reloadKey: month,
          load: () => AppScope.read(context).repo.dailyIncome(month.year, month.month),
          loading: const Padding(
            padding: IdaSizes.screenPadding,
            child: Skeleton(height: 200, radius: IdaRadius.xlR),
          ),
          builder: (context, days, _) {
            if (days.isEmpty) return EmptyState(icon: Icons.event_busy_outlined, title: s.noData);
            final total = days.fold(0.0, (a, d) => a + d.amount);
            return IdaCard(
              padding: const EdgeInsets.symmetric(vertical: IdaSpace.s2),
              child: Column(
                children: [
                  for (final (i, d) in days.indexed) ...[
                    if (i > 0) Divider(indent: IdaSpace.s4, endIndent: IdaSpace.s4, color: p.border),
                    Padding(
                      padding: const EdgeInsets.symmetric(horizontal: IdaSpace.s4, vertical: IdaSpace.s2),
                      child: Row(
                        children: [
                          DateBlock(d.date, highlight: !d.confirmed),
                          const SizedBox(width: IdaSpace.s3),
                          Expanded(
                            child: Column(
                              crossAxisAlignment: CrossAxisAlignment.start,
                              children: [
                                Text(s.cases(d.cases), style: context.text.bodyMedium),
                                if (!d.confirmed)
                                  IdaBadge(
                                    label: s.unconfirmed,
                                    tone: IdaBadgeTone.neutral,
                                    icon: Icons.schedule_rounded,
                                  ),
                              ],
                            ),
                          ),

                          IdaAmount(
                            d.amount,
                            symbol: true,
                            hidden: hidden,
                            style: context.text.titleSmall!.copyWith(color: d.confirmed ? null : p.textSecondary),
                          ),
                        ],
                      ),
                    ),
                  ],
                  Container(
                    margin: const EdgeInsets.only(top: IdaSpace.s2),
                    padding: const EdgeInsets.fromLTRB(IdaSpace.s4, IdaSpace.s3, IdaSpace.s4, IdaSpace.s2),
                    decoration: BoxDecoration(
                      color: p.surface2,
                      border: Border(top: BorderSide(color: p.dividerStrong, width: 2)),
                    ),
                    child: Row(
                      children: [
                        Expanded(
                          child: Text(
                            '${s.all} · ${Fmt.monthYear(month.year, month.month, s.en)}',
                            style: context.text.labelLarge,
                          ),
                        ),
                        IdaAmount(
                          total,
                          symbol: true,
                          hidden: hidden,
                          style: context.text.titleSmall!.copyWith(fontWeight: FontWeight.w700),
                        ),
                      ],
                    ),
                  ),
                ],
              ),
            );
          },
        ),
      ],
    );
  }
}

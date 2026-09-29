import 'package:flutter/material.dart';

import '../../app/app_controller.dart';
import '../../core/format.dart';
import '../../data/models.dart';
import '../../l10n/strings.dart';
import '../../ui/components.dart';
import 'documents_screen.dart';


class PaySlipScreen extends StatelessWidget {
  const PaySlipScreen({super.key, required this.year, required this.month});
  final int year;
  final int month;

  @override
  Widget build(BuildContext context) {
    final s = S.of(context);
    return AsyncView<PaySlip>(
      load: () => AppScope.read(context).repo.paySlip(year, month),
      loading: Scaffold(appBar: AppBar(title: Text(s.paySlip)), body: const SkeletonList(header: true)),
      builder: (context, slip, reload) => Scaffold(
        appBar: AppBar(title: Text(s.paySlip)),
        body: RefreshIndicator(
          onRefresh: reload,
          child: ListView(
            padding: const EdgeInsets.fromLTRB(IdaSpace.s4, IdaSpace.s4, IdaSpace.s4, IdaSpace.s8),
            children: [
              ContentWidth(
                child: Column(crossAxisAlignment: CrossAxisAlignment.stretch, children: [
                  _SlipHeader(slip: slip),
                  const SizedBox(height: IdaSpace.s3),
                  const _Confidential(),
                  const SizedBox(height: IdaSpace.s3),
                  Row(children: [
                    Expanded(child: _NetTile(label: s.summary406, value: slip.net406)),
                    const SizedBox(width: IdaSpace.s3),
                    Expanded(child: _NetTile(label: s.summary402, value: slip.net402)),
                  ]),
                  for (final sec in slip.sections) ...[
                    const SizedBox(height: IdaSpace.s3),
                    _SectionTable(section: sec),
                  ],
                  const SizedBox(height: IdaSpace.s3),
                  _PaymentCard(slip: slip),
                ]),
              ),
            ],
          ),
        ),
        bottomNavigationBar: BottomActionBar(
          child: PrimaryButton(
            key: const Key('email-slip'),
            label: s.sendToEmail,
            icon: Icons.forward_to_inbox_rounded,
            onPressed: () => requestDocument(context, DocumentKind.paySlip, year: year, month: month),
          ),
        ),
      ),
    );
  }
}

class _SlipHeader extends StatelessWidget {
  const _SlipHeader({required this.slip});
  final PaySlip slip;

  @override
  Widget build(BuildContext context) {
    final s = S.of(context);
    final hidden = AppScope.of(context).amountsHidden;
    const white = IdaColors.textInverse;
    return Container(
      padding: const EdgeInsets.all(IdaSpace.s5),
      decoration: BoxDecoration(
        gradient: IdaColors.gradientCardAccent,
        borderRadius: IdaRadius.xlR,
        boxShadow: context.ida.shadowMd,
      ),
      child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
        Text(s.paySlipFor(Fmt.monthYear(slip.year, slip.month, s.en)),
            style: context.text.titleSmall!.copyWith(color: white)),
        Text('${slip.hospitalNameTh} · ${slip.doctorName}',
            style: context.text.bodySmall!.copyWith(color: white.withValues(alpha: 0.92))),
        const SizedBox(height: IdaSpace.s4),
        Text(s.netTotal, style: context.text.bodyMedium!.copyWith(color: white.withValues(alpha: 0.92))),
        IdaAmount(slip.total,
            symbol: true,
            hidden: hidden,
            textAlign: TextAlign.left,
            style: context.text.displaySmall!.copyWith(color: white)),
        const SizedBox(height: IdaSpace.s2),
        Row(children: [
          Icon(slip.paidOn != null ? Icons.check_circle_rounded : Icons.schedule_rounded, size: IdaSizes.iconSm, color: white),
          const SizedBox(width: IdaSpace.s1),
          Text(
            slip.paidOn != null ? s.paidOn(Fmt.date(slip.paidOn!, s.en)) : s.awaitingPay,
            style: context.text.labelLarge!.copyWith(color: white),
          ),
        ]),
      ]),
    );
  }
}

class _Confidential extends StatelessWidget {
  const _Confidential();

  @override
  Widget build(BuildContext context) {
    final p = context.ida;
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: IdaSpace.s3, vertical: IdaSpace.s2),
      decoration: BoxDecoration(color: p.warningSoft, borderRadius: IdaRadius.mdR),
      child: Row(children: [
        Icon(Icons.lock_outline_rounded, size: IdaSizes.iconSm, color: p.warningText),
        const SizedBox(width: IdaSpace.s2),
        Flexible(
          child: Text('Confidential · ${S.of(context).confidential}',
              style: context.text.labelMedium!.copyWith(color: p.warningText)),
        ),
      ]),
    );
  }
}

class _NetTile extends StatelessWidget {
  const _NetTile({required this.label, required this.value});
  final String label;
  final double value;

  @override
  Widget build(BuildContext context) => IdaCard(
        padding: const EdgeInsets.all(IdaSpace.s4),
        child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
          Text(label, style: context.text.bodySmall),
          const SizedBox(height: 2),
          IdaAmount(value,
              hidden: AppScope.of(context).amountsHidden,
              textAlign: TextAlign.left,
              style: context.text.titleMedium),
        ]),
      );
}


class _SectionTable extends StatelessWidget {
  const _SectionTable({required this.section});
  final PaySlipSection section;

  @override
  Widget build(BuildContext context) {
    final p = context.ida;
    final hidden = AppScope.of(context).amountsHidden;
    final cols = section.columns.length;
    final colW = cols >= 3 ? 76.0 : 96.0;

    Widget values(List<double?> v, {bool bold = false}) => Row(mainAxisSize: MainAxisSize.min, children: [
          for (var i = 0; i < cols; i++)
            SizedBox(
              width: colW,
              child: i < v.length && v[i] != null
                  ? FittedBox(
                      alignment: Alignment.centerRight,
                      fit: BoxFit.scaleDown,
                      child: IdaAmount(
                        v[i]!,
                        colored: v[i]! < 0,
                        hidden: hidden,
                        style: context.text.bodyMedium!.copyWith(fontWeight: bold ? FontWeight.w700 : FontWeight.w400),
                      ),
                    )
                  : Text('–', textAlign: TextAlign.right, style: context.text.bodyMedium!.copyWith(color: p.textMuted)),
            ),
        ]);

    return IdaCard(
      padding: EdgeInsets.zero,
      child: Column(crossAxisAlignment: CrossAxisAlignment.stretch, children: [
        Padding(
          padding: const EdgeInsets.fromLTRB(IdaSpace.s4, IdaSpace.s4, IdaSpace.s4, IdaSpace.s2),
          child: Text(section.title, style: context.text.titleSmall),
        ),
        Container(
          color: p.surface2,
          padding: const EdgeInsets.symmetric(horizontal: IdaSpace.s4, vertical: IdaSpace.s2),
          child: Row(children: [
            const Spacer(),
            for (final c in section.columns)
              SizedBox(
                width: colW,
                child: Text(c, textAlign: TextAlign.right, style: context.text.labelMedium),
              ),
          ]),
        ),
        for (final r in section.rows)
          Container(
            decoration: BoxDecoration(
              color: r.total ? p.surface2 : null,
              border: Border(
                top: BorderSide(color: r.total ? p.dividerStrong : p.border, width: r.total ? 2 : 1),
              ),
            ),
            padding: const EdgeInsets.symmetric(horizontal: IdaSpace.s4, vertical: IdaSpace.s3),
            child: Row(crossAxisAlignment: CrossAxisAlignment.start, children: [
              Expanded(
                child: Text(
                  r.label,
                  style: r.total ? context.text.labelLarge : context.text.bodyMedium!.copyWith(color: p.textSecondary),
                ),
              ),
              values(r.values, bold: r.total),
            ]),
          ),
      ]),
    );
  }
}

class _PaymentCard extends StatelessWidget {
  const _PaymentCard({required this.slip});
  final PaySlip slip;

  @override
  Widget build(BuildContext context) {
    final s = S.of(context);
    final hidden = AppScope.of(context).amountsHidden;
    return IdaCard(
      child: Column(crossAxisAlignment: CrossAxisAlignment.stretch, children: [
        Text(s.paymentInfo, style: context.text.titleSmall),
        const SizedBox(height: IdaSpace.s2),
        KeyValue(s.taxId, Fmt.maskNationalId(slip.taxId), numeric: true),
        KeyValue(s.payType, slip.payType),
        KeyValue(s.accountNo, Fmt.maskAccount(slip.accountNo), numeric: true),
        KeyValue(s.bank, slip.bankNameTh),
        KeyValue(s.accountName, slip.accountName),
        const SizedBox(height: IdaSpace.s2),
        Divider(color: context.ida.border),
        const SizedBox(height: IdaSpace.s2),
        Text(s.accumulated402, style: context.text.titleSmall),
        const SizedBox(height: IdaSpace.s2),
        Row(children: [
          Expanded(child: Text(s.accumulatedIncome, style: context.text.bodyMedium)),
          IdaAmount(slip.accumulated402Income, hidden: hidden, style: context.text.bodyMedium),
        ]),
        const SizedBox(height: IdaSpace.s2),
        Row(children: [
          Expanded(child: Text(s.accumulatedTax, style: context.text.bodyMedium)),
          IdaAmount(slip.accumulated402Tax, hidden: hidden, style: context.text.bodyMedium),
        ]),
      ]),
    );
  }
}

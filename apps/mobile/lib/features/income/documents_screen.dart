import 'package:flutter/material.dart';

import '../../app/app_controller.dart';
import '../../core/format.dart';
import '../../data/models.dart';
import '../../l10n/strings.dart';
import '../../ui/components.dart';
import '../../ui/status.dart';


class DocumentsScreen extends StatefulWidget {
  const DocumentsScreen({super.key});

  @override
  State<DocumentsScreen> createState() => _DocumentsScreenState();
}

class _DocumentsScreenState extends State<DocumentsScreen> {
  final _history = GlobalKey<AsyncViewState<List<DocumentRequestRecord>>>();

  Future<void> _request(DocumentKind kind) async {
    final now = DateTime.now();
    final ok = await requestDocument(
      context,
      kind,
      year: kind == DocumentKind.withholdingTax ? now.year - 1 : now.year,
      month: kind == DocumentKind.paySlip ? (now.month == 1 ? 12 : now.month - 1) : 1,
      toMonth: now.month,
    );
    if (ok) await _history.currentState?.reload();
  }

  @override
  Widget build(BuildContext context) {
    final s = S.of(context);
    final p = context.ida;
    return Scaffold(
      appBar: AppBar(title: Text(s.menuDocuments)),
      body: ListView(
        padding: const EdgeInsets.fromLTRB(IdaSpace.s4, IdaSpace.s4, IdaSpace.s4, IdaSpace.s8),
        children: [
          ContentWidth(
            child: Column(crossAxisAlignment: CrossAxisAlignment.stretch, children: [
              Text(s.documentsTitle, style: context.text.titleLarge),
              const SizedBox(height: IdaSpace.s2),
              const IdaBrandTick(),
              const SizedBox(height: IdaSpace.s2),
              Text(s.documentsIntro, style: context.text.bodyMedium!.copyWith(color: p.textSecondary)),
              const SizedBox(height: IdaSpace.s4),
              for (final k in DocumentKind.values) ...[
                IdaCard(
                  padding: const EdgeInsets.all(IdaSpace.s4),
                  onTap: () => _request(k),
                  child: Row(children: [
                    IconBox(documentIcon(k)),
                    const SizedBox(width: IdaSpace.s3),
                    Expanded(
                      child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                        Text(s.docKind(k), style: context.text.titleSmall),
                        const SizedBox(height: 2),
                        Text(s.docKindHint(k), style: context.text.bodySmall),
                      ]),
                    ),
                    Icon(Icons.chevron_right_rounded, color: p.borderStrong),
                  ]),
                ),
                const SizedBox(height: IdaSpace.s3),
              ],
              const SizedBox(height: IdaSpace.s3),
              SectionHeader(s.recentRequests),
              AsyncView<List<DocumentRequestRecord>>(
                key: _history,
                load: () => AppScope.read(context).repo.documentHistory(),
                loading: const Skeleton(height: 120, radius: IdaRadius.xlR),
                builder: (context, list, _) => list.isEmpty
                    ? EmptyState(icon: Icons.inbox_outlined, title: s.noDocuments)
                    : IdaCard(
                        padding: const EdgeInsets.symmetric(vertical: IdaSpace.s2),
                        child: Column(children: [
                          for (final (i, d) in list.indexed) ...[
                            if (i > 0) Divider(indent: IdaSpace.s4, endIndent: IdaSpace.s4, color: p.border),
                            Padding(
                              padding: const EdgeInsets.symmetric(horizontal: IdaSpace.s4, vertical: IdaSpace.s2),
                              child: Row(children: [
                                IconBox(documentIcon(d.kind), size: IdaSizes.avatarSm),
                                const SizedBox(width: IdaSpace.s3),
                                Expanded(
                                  child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                                    Text(s.docKind(d.kind),
                                        maxLines: 1, overflow: TextOverflow.ellipsis, style: context.text.bodyMedium),
                                    Text(
                                      '${s.en ? d.periodLabelEn : d.periodLabelTh} · ${Fmt.relative(d.requestedAt, s.en)}',
                                      style: context.text.bodySmall,
                                    ),
                                    Text(Fmt.maskEmail(d.email), style: context.text.bodySmall),
                                  ]),
                                ),
                                IdaBadge(label: s.sent, tone: IdaBadgeTone.success, icon: Icons.mark_email_read_outlined),
                              ]),
                            ),
                          ],
                        ]),
                      ),
              ),
            ]),
          ),
        ],
      ),
    );
  }
}


Future<bool> requestDocument(BuildContext context, DocumentKind kind, {required int year, int? month, int? toMonth}) async {
  final rec = await showModalBottomSheet<DocumentRequestRecord>(
    context: context,
    isScrollControlled: true,
    builder: (_) => _RequestSheet(kind: kind, year: year, month: month ?? 1, toMonth: toMonth ?? 12),
  );
  if (rec == null || !context.mounted) return false;
  showToast(context, S.of(context).docSent(Fmt.maskEmail(rec.email)));
  return true;
}

class _RequestSheet extends StatefulWidget {
  const _RequestSheet({required this.kind, required this.year, required this.month, required this.toMonth});
  final DocumentKind kind;
  final int year;
  final int month;
  final int toMonth;

  @override
  State<_RequestSheet> createState() => _RequestSheetState();
}

class _RequestSheetState extends State<_RequestSheet> {
  late int _year = widget.year;
  late int _from = widget.month;
  late int _to = widget.toMonth;
  bool _busy = false;
  String? _error;

  DateTime get _now => DateTime.now();


  bool _monthOk(int m) => _year < _now.year || m <= _now.month;

  List<int> get _years => widget.kind == DocumentKind.withholdingTax
      ? [_now.year - 1, _now.year - 2, _now.year - 3]
      : [_now.year, _now.year - 1, _now.year - 2];

  Future<void> _send() async {
    final s = S.of(context);
    if (widget.kind == DocumentKind.incomeCertificate && _to < _from) {
      setState(() => _error = s.invalidRange);
      return;
    }
    setState(() {
      _busy = true;
      _error = null;
    });
    try {
      final rec = await AppScope.read(context).repo.requestDocument(
            widget.kind,
            year: _year,
            month: widget.kind == DocumentKind.withholdingTax ? null : _from,
            toMonth: widget.kind == DocumentKind.incomeCertificate ? _to : null,
          );
      if (mounted) Navigator.pop(context, rec);
    } catch (e) {
      if (mounted) setState(() => _error = errorText(context, e));
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  Future<int?> _pickMonth(String title, int current) => pickOption<int>(
        context,
        title: title,
        selected: current,
        enabled: _monthOk,
        options: [for (var m = 1; m <= 12; m++) (m, Fmt.month(m, S.of(context).en))],
      );

  @override
  Widget build(BuildContext context) {
    final s = S.of(context);
    final p = context.ida;
    final app = AppScope.of(context);
    final en = s.en;
    return Padding(
      padding: EdgeInsets.fromLTRB(IdaSpace.s5, 0, IdaSpace.s5, IdaSpace.s5 + MediaQuery.viewInsetsOf(context).bottom),
      child: SafeArea(
        child: Column(mainAxisSize: MainAxisSize.min, crossAxisAlignment: CrossAxisAlignment.stretch, children: [
          Row(children: [
            IconBox(documentIcon(widget.kind), size: IdaSizes.avatarMd),
            const SizedBox(width: IdaSpace.s3),
            Expanded(child: Text(s.docKind(widget.kind), style: context.text.titleMedium)),
          ]),
          const SizedBox(height: IdaSpace.s5),
          IdaSelectField(
            label: widget.kind == DocumentKind.withholdingTax ? s.taxYear : s.year,
            value: '${Fmt.year(_year, en)}',
            icon: Icons.calendar_today_outlined,
            onTap: () async {
              final y = await pickOption<int>(
                context,
                title: s.year,
                selected: _year,
                options: [for (final y in _years) (y, '${Fmt.year(y, en)}')],
              );
              if (y != null) {
                setState(() {
                  _year = y;
                  if (!_monthOk(_from)) _from = _now.month;
                  if (!_monthOk(_to)) _to = _now.month;
                });
              }
            },
          ),
          if (widget.kind != DocumentKind.withholdingTax) ...[
            const SizedBox(height: IdaSpace.s4),
            Row(children: [
              Expanded(
                child: IdaSelectField(
                  label: widget.kind == DocumentKind.paySlip ? s.month : s.fromMonth,
                  value: Fmt.month(_from, en),
                  onTap: () async {
                    final m = await _pickMonth(s.fromMonth, _from);
                    if (m != null) setState(() => _from = m);
                  },
                ),
              ),
              if (widget.kind == DocumentKind.incomeCertificate) ...[
                const SizedBox(width: IdaSpace.s3),
                Expanded(
                  child: IdaSelectField(
                    label: s.toMonth,
                    value: Fmt.month(_to, en),
                    onTap: () async {
                      final m = await _pickMonth(s.toMonth, _to);
                      if (m != null) setState(() => _to = m);
                    },
                  ),
                ),
              ],
            ]),
          ],
          const SizedBox(height: IdaSpace.s4),
          KeyValue(s.sendTo, Fmt.maskEmail(app.requireSession.user.email)),
          const SizedBox(height: IdaSpace.s2),
          Container(
            padding: const EdgeInsets.all(IdaSpace.s3),
            decoration: BoxDecoration(color: p.accentSoft, borderRadius: IdaRadius.mdR),
            child: Row(crossAxisAlignment: CrossAxisAlignment.start, children: [
              Icon(Icons.key_rounded, size: IdaSizes.iconMd, color: p.accentStrong),
              const SizedBox(width: IdaSpace.s2),
              Expanded(
                child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                  Text(s.pdfPassword, style: context.text.labelLarge!.copyWith(color: p.accentStrong)),
                  Text(s.pdfPasswordHint, style: context.text.bodySmall!.copyWith(color: p.accentStrong)),
                ]),
              ),
            ]),
          ),
          if (_error != null) ...[
            const SizedBox(height: IdaSpace.s3),
            Text(_error!, style: context.text.bodyMedium!.copyWith(color: p.dangerText)),
          ],
          const SizedBox(height: IdaSpace.s5),
          PrimaryButton(
            key: const Key('send-document'),
            label: s.sendDocument,
            icon: Icons.send_rounded,
            loading: _busy,
            onPressed: _send,
          ),
        ]),
      ),
    );
  }
}

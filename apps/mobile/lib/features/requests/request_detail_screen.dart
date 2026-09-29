import 'package:flutter/material.dart';

import '../../app/app_controller.dart';
import '../../core/format.dart';
import '../../data/models.dart';
import '../../l10n/strings.dart';
import '../../ui/components.dart';
import '../../ui/status.dart';
import 'request_list_screen.dart';


class RequestDetailScreen extends StatefulWidget {
  const RequestDetailScreen({super.key, required this.id});
  final String id;

  @override
  State<RequestDetailScreen> createState() => _RequestDetailScreenState();
}

class _RequestDetailScreenState extends State<RequestDetailScreen> {
  final _view = GlobalKey<AsyncViewState<ApprovalDetail>>();

  Future<void> _decide(Decision d) async {
    if (await decideRequests(context, [widget.id], d) && mounted) {
      await _view.currentState?.reload();
    }
  }

  @override
  Widget build(BuildContext context) {
    final s = S.of(context);
    return AsyncView<ApprovalDetail>(
      key: _view,
      load: () => AppScope.read(context).repo.approvalDetail(widget.id),
      loading: Scaffold(appBar: AppBar(title: Text(s.details)), body: const SkeletonList(header: true)),
      builder: (context, d, reload) {
        final r = d.request;
        return Scaffold(
          appBar: AppBar(title: Text(s.requestType(r.requestType))),
          body: RefreshIndicator(
            onRefresh: reload,
            child: ListView(
              padding: const EdgeInsets.fromLTRB(IdaSpace.s4, IdaSpace.s4, IdaSpace.s4, IdaSpace.s8),
              children: [
                ContentWidth(
                  child: Column(crossAxisAlignment: CrossAxisAlignment.stretch, children: [
                    _Header(detail: d),
                    if (d.reason != null) ...[
                      const SizedBox(height: IdaSpace.s3),
                      _NoteCard(title: s.noteFromRequester, text: d.reason!, icon: Icons.format_quote_rounded),
                    ],
                    const SizedBox(height: IdaSpace.s3),
                    _ChangesCard(changes: d.changes),
                    if (d.attachments.isNotEmpty) ...[
                      const SizedBox(height: IdaSpace.s3),
                      _AttachmentsCard(files: d.attachments),
                    ],
                    const SizedBox(height: IdaSpace.s3),
                    _Timeline(detail: d),
                  ]),
                ),
              ],
            ),
          ),
          bottomNavigationBar: !d.canDecide
              ? null
              : BottomActionBar(
                  child: Row(children: [
                    Expanded(
                      child: OutlinedButton.icon(
                        key: const Key('detail-reject'),
                        style: OutlinedButton.styleFrom(
                          foregroundColor: context.ida.dangerText,
                          side: BorderSide(color: context.ida.dangerText, width: 1.5),
                        ),
                        onPressed: () => _decide(Decision.reject),
                        icon: const Icon(Icons.close_rounded, size: IdaSizes.iconMd),
                        label: Text(s.reject),
                      ),
                    ),
                    const SizedBox(width: IdaSpace.s3),
                    Expanded(
                      child: PrimaryButton(
                        key: const Key('detail-approve'),
                        label: s.approve,
                        icon: Icons.check_rounded,
                        onPressed: () => _decide(Decision.approve),
                      ),
                    ),
                  ]),
                ),
        );
      },
    );
  }
}

class _Header extends StatelessWidget {
  const _Header({required this.detail});
  final ApprovalDetail detail;

  @override
  Widget build(BuildContext context) {
    final s = S.of(context);
    final r = detail.request;
    return IdaCard(
      child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
        Row(crossAxisAlignment: CrossAxisAlignment.start, children: [
          IconBox(requestTypeIcon(r.requestType)),
          const SizedBox(width: IdaSpace.s3),
          Expanded(
            child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
              Text(r.summary, style: context.text.titleMedium),
              const SizedBox(height: IdaSpace.s1),
              StatusBadge(r.status),
            ]),
          ),
        ]),
        const SizedBox(height: IdaSpace.s3),
        Divider(color: context.ida.border),
        KeyValue(s.requestNo, r.requestNo, numeric: true),
        KeyValue(s.requester, r.requestedBy),
        KeyValue(s.submittedAt, '${Fmt.dateLong(r.requestedAt, s.en)} ${Fmt.time(r.requestedAt)}'),
        if (r.currentStepRoleTh != null) KeyValue(s.filterStatus, s.waitingAt(r.currentStepRoleTh!)),
      ]),
    );
  }
}

class _NoteCard extends StatelessWidget {
  const _NoteCard({required this.title, required this.text, required this.icon, this.tone});
  final String title;
  final String text;
  final IconData icon;
  final IdaBadgeTone? tone;

  @override
  Widget build(BuildContext context) {
    final p = context.ida;
    final (bg, fg) = tone == IdaBadgeTone.error ? (p.dangerSoft, p.dangerText) : (p.surface2, p.textSecondary);
    return Container(
      padding: const EdgeInsets.all(IdaSpace.s4),
      decoration: BoxDecoration(color: bg, borderRadius: IdaRadius.lgR),
      child: Row(crossAxisAlignment: CrossAxisAlignment.start, children: [
        Icon(icon, size: IdaSizes.iconMd, color: fg),
        const SizedBox(width: IdaSpace.s3),
        Expanded(
          child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
            Text(title, style: context.text.labelMedium!.copyWith(color: fg)),
            const SizedBox(height: 2),
            Text(text, style: context.text.bodyMedium!.copyWith(color: tone == IdaBadgeTone.error ? fg : p.text)),
          ]),
        ),
      ]),
    );
  }
}

class _ChangesCard extends StatelessWidget {
  const _ChangesCard({required this.changes});
  final List<FieldChange> changes;

  @override
  Widget build(BuildContext context) {
    final s = S.of(context);
    final p = context.ida;
    return IdaCard(
      child: Column(crossAxisAlignment: CrossAxisAlignment.stretch, children: [
        Text(s.changes, style: context.text.titleSmall),
        const SizedBox(height: IdaSpace.s3),
        for (final (i, c) in changes.indexed) ...[
          if (i > 0) Divider(height: IdaSpace.s6, color: p.border),
          Text(c.label, style: context.text.labelLarge!.copyWith(color: p.textSecondary)),
          const SizedBox(height: IdaSpace.s2),
          _DiffLine(tag: s.before, text: c.before, old: true),
          const SizedBox(height: IdaSpace.s1),
          _DiffLine(tag: s.after, text: c.after, old: false),
        ],
      ]),
    );
  }
}


class _DiffLine extends StatelessWidget {
  const _DiffLine({required this.tag, required this.text, required this.old});
  final String tag;
  final String text;
  final bool old;

  @override
  Widget build(BuildContext context) {
    final p = context.ida;
    return Row(crossAxisAlignment: CrossAxisAlignment.start, children: [
      SizedBox(
        width: IdaSpace.s12,
        child: IdaBadge(label: tag, tone: old ? IdaBadgeTone.neutral : IdaBadgeTone.info),
      ),
      const SizedBox(width: IdaSpace.s2),
      Expanded(
        child: Text(
          text,
          style: old
              ? context.text.bodyMedium!.copyWith(
                  color: p.textSecondary, decoration: TextDecoration.lineThrough, decorationColor: p.textMuted)
              : context.text.bodyLarge!.copyWith(fontWeight: FontWeight.w600),
        ),
      ),
    ]);
  }
}

class _AttachmentsCard extends StatelessWidget {
  const _AttachmentsCard({required this.files});
  final List<Attachment> files;

  @override
  Widget build(BuildContext context) {
    final s = S.of(context);
    return IdaCard(
      padding: const EdgeInsets.fromLTRB(IdaSpace.s5, IdaSpace.s5, IdaSpace.s3, IdaSpace.s3),
      child: Column(crossAxisAlignment: CrossAxisAlignment.stretch, children: [
        Text(s.attachments, style: context.text.titleSmall),
        const SizedBox(height: IdaSpace.s2),
        for (final f in files)
          FileRow(name: f.name, detail: Fmt.fileSize(f.sizeKb)),
      ]),
    );
  }
}


class FileRow extends StatelessWidget {
  const FileRow({super.key, required this.name, required this.detail, this.onRemove});
  final String name;
  final String detail;
  final VoidCallback? onRemove;

  @override
  Widget build(BuildContext context) {
    final s = S.of(context);
    final p = context.ida;
    final isPdf = name.toLowerCase().endsWith('.pdf');
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: IdaSpace.s1),
      child: Row(children: [
        IconBox(
          isPdf ? Icons.picture_as_pdf_outlined : Icons.image_outlined,
          size: IdaSizes.avatarSm,
          bg: isPdf ? p.dangerSoft : p.accentSoft,
          fg: isPdf ? p.dangerText : p.accentStrong,
        ),
        const SizedBox(width: IdaSpace.s3),
        Expanded(
          child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
            Text(name, maxLines: 1, overflow: TextOverflow.ellipsis, style: context.text.bodyMedium),
            Text(detail, style: context.text.bodySmall),
          ]),
        ),
        if (onRemove != null)
          IconButton(tooltip: s.removeFile, onPressed: onRemove, icon: Icon(Icons.delete_outline_rounded, color: p.dangerText))
        else
          TextButton(
            onPressed: () => showToast(context, s.documentPreviewNote, tone: ToastTone.info),
            child: Text(s.view),
          ),
      ]),
    );
  }
}


class _Timeline extends StatelessWidget {
  const _Timeline({required this.detail});
  final ApprovalDetail detail;

  @override
  Widget build(BuildContext context) {
    final s = S.of(context);
    final p = context.ida;
    final r = detail.request;
    final rows = <Widget>[
      _TimelineRow(
        icon: Icons.send_rounded,
        bg: p.primarySoft,
        fg: p.primaryText,
        title: s.submitted,
        subtitle: '${r.requestedBy} · ${Fmt.date(r.requestedAt, s.en)} ${Fmt.time(r.requestedAt)}',
        last: detail.steps.isEmpty,
        passed: true,
      ),
      for (final (i, st) in detail.steps.indexed)
        _TimelineRow(
          icon: switch (st.action) {
            StepAction.approve => Icons.check_rounded,
            StepAction.reject => Icons.close_rounded,
            StepAction.returned => Icons.undo_rounded,
            null => Icons.hourglass_top_rounded,
          },
          bg: switch (st.action) {
            StepAction.approve => p.successSoft,
            StepAction.reject => p.dangerSoft,
            StepAction.returned => p.warningSoft,
            null => p.surface2,
          },
          fg: switch (st.action) {
            StepAction.approve => p.successText,
            StepAction.reject => p.dangerText,
            StepAction.returned => p.warningText,
            null => p.textSecondary,
          },
          title: st.roleNameTh,
          subtitle: st.action == null
              ? s.waiting
              : '${switch (st.action!) {
                  StepAction.approve => s.status(ApprovalStatus.approved),
                  StepAction.reject => s.status(ApprovalStatus.rejected),
                  StepAction.returned => s.status(ApprovalStatus.returned),
                }} · ${st.user ?? '-'} · ${st.at == null ? '' : '${Fmt.date(st.at!, s.en)} ${Fmt.time(st.at!)}'}',
          comment: st.comment,
          commentTone: st.action == StepAction.reject ? IdaBadgeTone.error : null,
          last: i == detail.steps.length - 1,
          passed: st.action == StepAction.approve,
        ),
    ];
    return IdaCard(
      child: Column(crossAxisAlignment: CrossAxisAlignment.stretch, children: [
        Text(s.timeline, style: context.text.titleSmall),
        const SizedBox(height: IdaSpace.s4),
        ...rows,
      ]),
    );
  }
}

class _TimelineRow extends StatelessWidget {
  const _TimelineRow({
    required this.icon,
    required this.bg,
    required this.fg,
    required this.title,
    required this.subtitle,
    required this.last,
    required this.passed,
    this.comment,
    this.commentTone,
  });

  final IconData icon;
  final Color bg;
  final Color fg;
  final String title;
  final String subtitle;
  final bool last;
  final bool passed;
  final String? comment;
  final IdaBadgeTone? commentTone;

  @override
  Widget build(BuildContext context) {
    final p = context.ida;
    return IntrinsicHeight(
      child: Row(crossAxisAlignment: CrossAxisAlignment.stretch, children: [
        Column(children: [
          Container(
            width: IdaSizes.avatarSm,
            height: IdaSizes.avatarSm,
            decoration: BoxDecoration(color: bg, shape: BoxShape.circle),
            child: Icon(icon, size: IdaSizes.iconMd, color: fg),
          ),
          if (!last)
            Expanded(
              child: Container(
                width: 2,
                margin: const EdgeInsets.symmetric(vertical: IdaSpace.s1),

                color: passed ? p.success : p.border,
              ),
            ),
        ]),
        const SizedBox(width: IdaSpace.s3),
        Expanded(
          child: Padding(
            padding: EdgeInsets.only(bottom: last ? 0 : IdaSpace.s5),
            child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
              Text(title, style: context.text.titleSmall),
              Text(subtitle, style: context.text.bodySmall),
              if (comment != null) ...[
                const SizedBox(height: IdaSpace.s2),
                _NoteCard(
                  title: S.of(context).noteFromApprover,
                  text: comment!,
                  icon: Icons.chat_bubble_outline_rounded,
                  tone: commentTone,
                ),
              ],
            ]),
          ),
        ),
      ]),
    );
  }
}

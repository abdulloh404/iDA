import 'package:flutter/material.dart';

import '../../core/format.dart';
import '../../data/models.dart';
import '../../l10n/strings.dart';
import '../../ui/components.dart';
import '../../ui/status.dart';


class RequestTile extends StatelessWidget {
  const RequestTile({
    super.key,
    required this.request,
    required this.onTap,
    this.showRequester = false,
    this.onApprove,
    this.onReject,
    this.selected,
  });

  final ApprovalRequest request;
  final VoidCallback onTap;
  final bool showRequester;
  final VoidCallback? onApprove;
  final VoidCallback? onReject;
  final bool? selected;

  @override
  Widget build(BuildContext context) {
    final s = S.of(context);
    final p = context.ida;
    final r = request;
    final isSelected = selected ?? false;
    return Semantics(
      selected: selected,
      child: AnimatedContainer(
        duration: IdaMotion.of(context, IdaMotion.fast),
        decoration: BoxDecoration(
          color: isSelected ? p.primarySoft : p.surface,
          borderRadius: IdaRadius.xlR,
          border: Border.all(color: isSelected ? p.primaryText : (context.isDark ? p.border : Colors.transparent)),
          boxShadow: isSelected ? null : p.shadowMd,
        ),
        child: Material(
          type: MaterialType.transparency,
          child: InkWell(
            borderRadius: IdaRadius.xlR,
            onTap: onTap,
            child: Padding(
              padding: const EdgeInsets.fromLTRB(IdaSpace.s4, IdaSpace.s4, IdaSpace.s3, IdaSpace.s4),
              child: Row(crossAxisAlignment: CrossAxisAlignment.start, children: [
                if (selected != null)
                  Padding(
                    padding: const EdgeInsets.only(right: IdaSpace.s2),
                    child: Icon(
                      isSelected ? Icons.check_box_rounded : Icons.check_box_outline_blank_rounded,
                      color: isSelected ? p.primaryText : p.borderStrong,
                    ),
                  )
                else
                  Padding(
                    padding: const EdgeInsets.only(right: IdaSpace.s3),
                    child: IconBox(requestTypeIcon(r.requestType), size: IdaSizes.avatarSm + IdaSpace.s1),
                  ),
                Expanded(
                  child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                    Text(r.summary, maxLines: 2, overflow: TextOverflow.ellipsis, style: context.text.titleSmall),
                    const SizedBox(height: 2),
                    Text(
                      '${s.requestType(r.requestType)} · ${r.requestNo}',
                      maxLines: 1,
                      overflow: TextOverflow.ellipsis,
                      style: idaNumeric(context.text.bodySmall!),
                    ),
                    if (showRequester)
                      Padding(
                        padding: const EdgeInsets.only(top: 2),
                        child: Row(children: [
                          Icon(Icons.person_outline_rounded, size: IdaSizes.iconXs, color: p.textSecondary),
                          const SizedBox(width: IdaSpace.s1),
                          Flexible(
                            child: Text(r.requestedBy,
                                maxLines: 1,
                                overflow: TextOverflow.ellipsis,
                                style: context.text.bodySmall!.copyWith(color: p.text)),
                          ),
                        ]),
                      ),
                    const SizedBox(height: IdaSpace.s2),
                    Wrap(
                      spacing: IdaSpace.s2,
                      runSpacing: IdaSpace.s1,
                      crossAxisAlignment: WrapCrossAlignment.center,
                      children: [
                        StatusBadge(r.status),
                        Text(Fmt.relative(r.requestedAt, s.en), style: idaNumeric(context.text.bodySmall!)),
                      ],
                    ),
                  ]),
                ),
                if (onApprove != null || onReject != null) ...[
                  const SizedBox(width: IdaSpace.s1),
                  Column(children: [
                    if (onApprove != null)
                      _RoundAction(
                        icon: Icons.check_rounded,
                        tooltip: s.quickApprove,
                        bg: p.successSoft,
                        fg: p.successText,
                        onTap: onApprove!,
                      ),
                    const SizedBox(height: IdaSpace.s2),
                    if (onReject != null)
                      _RoundAction(
                        icon: Icons.close_rounded,
                        tooltip: s.quickReject,
                        bg: p.dangerSoft,
                        fg: p.dangerText,
                        onTap: onReject!,
                      ),
                  ]),
                ] else if (selected == null)
                  Padding(
                    padding: const EdgeInsets.only(top: IdaSpace.s2),
                    child: Icon(Icons.chevron_right_rounded, color: p.borderStrong),
                  ),
              ]),
            ),
          ),
        ),
      ),
    );
  }
}

class _RoundAction extends StatelessWidget {
  const _RoundAction({required this.icon, required this.tooltip, required this.bg, required this.fg, required this.onTap});

  final IconData icon;
  final String tooltip;
  final Color bg;
  final Color fg;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) => Tooltip(
        message: tooltip,
        child: Material(
          color: bg,
          shape: const CircleBorder(),
          child: InkWell(
            customBorder: const CircleBorder(),
            onTap: onTap,
            child: SizedBox.square(
              dimension: IdaSizes.minTapTarget,
              child: Icon(icon, color: fg, size: IdaSizes.iconMd, semanticLabel: tooltip),
            ),
          ),
        ),
      );
}

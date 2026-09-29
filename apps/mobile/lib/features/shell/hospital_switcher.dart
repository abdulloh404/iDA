import 'package:flutter/material.dart';

import '../../app/app_controller.dart';
import '../../l10n/strings.dart';
import '../../ui/components.dart';


Future<void> showHospitalSwitcher(BuildContext context) async {
  final app = AppScope.read(context);
  final session = app.requireSession;
  final s = S.of(context);
  final picked = await showModalBottomSheet<String>(
    context: context,
    isScrollControlled: true,
    builder: (ctx) {
      final p = ctx.ida;
      final en = app.isEnglish;
      return SafeArea(
        child: Padding(
          padding: const EdgeInsets.fromLTRB(IdaSpace.s5, 0, IdaSpace.s5, IdaSpace.s4),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              Text(s.selectHospital, style: ctx.text.titleMedium),
              const SizedBox(height: IdaSpace.s1),
              Text(s.selectHospitalHint, style: ctx.text.bodyMedium!.copyWith(color: p.textSecondary)),
              const SizedBox(height: IdaSpace.s4),
              for (final h in session.hospitals) ...[
                _HospitalOption(
                  name: h.name(en),
                  detail: h.doctorCode ?? h.roleName(en),
                  selected: h.hospitalId == session.hospitalId,
                  onTap: () => Navigator.pop(ctx, h.hospitalId),
                ),
                const SizedBox(height: IdaSpace.s2),
              ],
            ],
          ),
        ),
      );
    },
  );
  if (picked == null || picked == session.hospitalId || !context.mounted) return;
  try {
    await app.switchHospital(picked);
    if (context.mounted) showToast(context, s.switchedTo(app.requireSession.hospital.name(app.isEnglish)), tone: ToastTone.info);
  } catch (e) {
    if (context.mounted) showErrorToast(context, e);
  }
}

class _HospitalOption extends StatelessWidget {
  const _HospitalOption({required this.name, required this.detail, required this.selected, required this.onTap});

  final String name;
  final String detail;
  final bool selected;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    final p = context.ida;
    return Semantics(
      selected: selected,
      button: true,
      child: Material(
        color: selected ? p.primarySoft : p.surface,
        shape: RoundedRectangleBorder(
          borderRadius: IdaRadius.lgR,
          side: BorderSide(color: selected ? p.primaryText : p.border, width: selected ? 1.5 : 1),
        ),
        child: InkWell(
          borderRadius: IdaRadius.lgR,
          onTap: onTap,
          child: Padding(
            padding: const EdgeInsets.all(IdaSpace.s4),
            child: Row(children: [
              IconBox(Icons.local_hospital_outlined, size: IdaSizes.avatarSm, bg: selected ? p.surface : p.primarySoft),
              const SizedBox(width: IdaSpace.s3),
              Expanded(
                child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                  Text(name, style: context.text.titleSmall!.copyWith(fontWeight: selected ? FontWeight.w700 : FontWeight.w600)),
                  Text(detail, style: idaNumeric(context.text.bodySmall!)),
                ]),
              ),
              Icon(
                selected ? Icons.radio_button_checked_rounded : Icons.radio_button_off_rounded,
                color: selected ? p.primaryText : p.borderStrong,
              ),
            ]),
          ),
        ),
      ),
    );
  }
}


class HospitalPill extends StatelessWidget {
  const HospitalPill({super.key});

  @override
  Widget build(BuildContext context) {
    final app = AppScope.of(context);
    final s = S.of(context);
    final session = app.requireSession;
    final h = session.hospital;
    final label = h.name(app.isEnglish);
    return Semantics(
      button: true,
      label: '${s.selectHospital}: $label',
      excludeSemantics: true,
      child: Material(
        color: IdaColors.textInverse.withValues(alpha: 0.16),
        shape: StadiumBorder(side: BorderSide(color: IdaColors.textInverse.withValues(alpha: 0.35))),
        child: InkWell(
          customBorder: const StadiumBorder(),
          onTap: session.hospitals.length > 1 ? () => showHospitalSwitcher(context) : null,
          child: ConstrainedBox(
            constraints: const BoxConstraints(minHeight: IdaSizes.minTapTarget),
            child: Padding(
              padding: const EdgeInsets.only(left: IdaSpace.s3, right: IdaSpace.s2),
              child: Row(mainAxisSize: MainAxisSize.min, children: [
                const Icon(Icons.local_hospital_rounded, size: IdaSizes.iconSm, color: IdaColors.textInverse),
                const SizedBox(width: IdaSpace.s2),
                Flexible(
                  child: Text(
                    h.doctorCode == null ? label : '$label · ${h.doctorCode}',
                    maxLines: 1,
                    overflow: TextOverflow.ellipsis,
                    style: context.text.labelLarge!.copyWith(color: IdaColors.textInverse),
                  ),
                ),
                if (session.hospitals.length > 1)
                  const Icon(Icons.expand_more_rounded, size: IdaSizes.iconMd, color: IdaColors.textInverse),
              ]),
            ),
          ),
        ),
      ),
    );
  }
}

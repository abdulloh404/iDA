import 'package:flutter/material.dart';

import '../core/format.dart';
import 'ida_colors.dart';
import 'ida_palette.dart';
import 'ida_theme.dart';


enum IdaBadgeTone { success, pending, error, info, closed, neutral }


class IdaBadge extends StatelessWidget {
  const IdaBadge({super.key, required this.label, required this.tone, this.icon});

  final String label;
  final IdaBadgeTone tone;
  final IconData? icon;

  @override
  Widget build(BuildContext context) {
    final p = context.ida;
    final (bg, fg) = switch (tone) {
      IdaBadgeTone.success => (p.successSoft, p.successText),
      IdaBadgeTone.pending => (p.warningSoft, p.warningText),
      IdaBadgeTone.error => (p.dangerSoft, p.dangerText),
      IdaBadgeTone.info => (p.accentSoft, p.accentStrong),
      IdaBadgeTone.closed => (p.primarySoft, p.primaryText),
      IdaBadgeTone.neutral => (p.surface2, p.textSecondary),
    };
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: IdaSpace.s2, vertical: 2),
      decoration: BoxDecoration(color: bg, borderRadius: IdaRadius.fullR),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          if (icon != null) ...[
            Icon(icon, size: IdaSizes.iconXs, color: fg),
            const SizedBox(width: IdaSpace.s1),
          ],
          Flexible(
            child: Text(
              label,
              maxLines: 1,
              overflow: TextOverflow.ellipsis,
              style: context.text.labelMedium!.copyWith(color: fg),
            ),
          ),
        ],
      ),
    );
  }
}


class IdaBrandBar extends StatelessWidget {
  const IdaBrandBar({super.key, this.height = IdaSizes.brandBar});

  final double height;

  @override
  Widget build(BuildContext context) => Container(
        height: height,
        decoration: const BoxDecoration(gradient: IdaColors.gradientBrand),
      );
}


class IdaBrandTick extends StatelessWidget {
  const IdaBrandTick({super.key});

  @override

  Widget build(BuildContext context) => Align(
        alignment: AlignmentDirectional.centerStart,
        child: Container(
          width: IdaSizes.brandTick,
          height: IdaSizes.brandBar,
          decoration: const BoxDecoration(gradient: IdaColors.gradientBrand, borderRadius: IdaRadius.fullR),
        ),
      );
}


class IdaHeroSurface extends StatelessWidget {
  const IdaHeroSurface({super.key, required this.child, this.overlay = true});

  final Widget child;
  final bool overlay;

  @override
  Widget build(BuildContext context) => DecoratedBox(
        decoration: const BoxDecoration(gradient: IdaColors.gradientHero),
        child: overlay
            ? DecoratedBox(
                decoration: const BoxDecoration(color: IdaColors.heroOverlay),
                child: child,
              )
            : child,
      );
}


class IdaAmount extends StatelessWidget {
  const IdaAmount(
    this.value, {
    super.key,
    this.colored = false,
    this.style,
    this.symbol = false,
    this.hidden = false,
    this.textAlign = TextAlign.right,
  });

  final double value;
  final bool colored;
  final TextStyle? style;
  final bool symbol;


  final bool hidden;
  final TextAlign textAlign;

  static String format(double value) => Fmt.money(value);

  @override
  Widget build(BuildContext context) {
    final s = idaAmountStyle(context, value: colored ? value : null, base: style);
    return Text(
      hidden ? '${symbol ? '฿' : ''}•••••••' : Fmt.money(value, symbol: symbol),
      textAlign: textAlign,
      maxLines: 1,
      style: s,
    );
  }
}


class IdaLogo extends StatelessWidget {
  const IdaLogo.mark({super.key, this.height = IdaSizes.logoMark, this.reversed = false, this.semanticLabel = 'iDA'})
      : _asset = 'assets/brand/ida-mark.png';

  const IdaLogo.lockup({super.key, this.height = IdaSizes.logoMark, this.reversed = false, this.semanticLabel = 'iDA Intelligent Doctor Application'})
      : _asset = 'assets/brand/ida-logo-horizontal.png';

  final String _asset;
  final double height;
  final bool reversed;
  final String semanticLabel;

  @override
  Widget build(BuildContext context) {
    final image = Image.asset(_asset, height: height, fit: BoxFit.contain, semanticLabel: semanticLabel);
    if (!reversed) return image;
    return ColorFiltered(
      colorFilter: const ColorFilter.mode(IdaColors.textInverse, BlendMode.srcIn),
      child: image,
    );
  }
}

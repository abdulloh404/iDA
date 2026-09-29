import 'package:flutter/material.dart';

import 'ida_colors.dart';


@immutable
class IdaPalette extends ThemeExtension<IdaPalette> {
  const IdaPalette({
    required this.primary,
    required this.primaryHover,
    required this.primaryActive,
    required this.primaryText,
    required this.primarySoft,
    required this.onPrimary,
    required this.accent,
    required this.accentStrong,
    required this.accentSoft,
    required this.success,
    required this.successText,
    required this.successSoft,
    required this.danger,
    required this.dangerText,
    required this.dangerSoft,
    required this.warning,
    required this.warningText,
    required this.warningSoft,
    required this.bg,
    required this.surface,
    required this.surface2,
    required this.border,
    required this.borderStrong,
    required this.dividerStrong,
    required this.skeleton,
    required this.text,
    required this.textSecondary,
    required this.textMuted,
    required this.textInverse,
    required this.link,
    required this.overlay,
    required this.focusRing,
    required this.chart,
    required this.shadowSm,
    required this.shadowMd,
    required this.shadowLg,
  });

  final Color primary;
  final Color primaryHover;
  final Color primaryActive;
  final Color primaryText;
  final Color primarySoft;
  final Color onPrimary;
  final Color accent;
  final Color accentStrong;
  final Color accentSoft;
  final Color success;
  final Color successText;
  final Color successSoft;
  final Color danger;
  final Color dangerText;
  final Color dangerSoft;
  final Color warning;
  final Color warningText;
  final Color warningSoft;
  final Color bg;
  final Color surface;
  final Color surface2;
  final Color border;
  final Color borderStrong;
  final Color dividerStrong;
  final Color skeleton;
  final Color text;
  final Color textSecondary;
  final Color textMuted;
  final Color textInverse;
  final Color link;
  final Color overlay;
  final Color focusRing;


  final List<Color> chart;

  final List<BoxShadow> shadowSm;
  final List<BoxShadow> shadowMd;
  final List<BoxShadow> shadowLg;

  static const light = IdaPalette(
    primary: IdaColors.primary,
    primaryHover: IdaColors.primaryHover,
    primaryActive: IdaColors.primaryActive,
    primaryText: IdaColors.primary,
    primarySoft: IdaColors.primarySoft,
    onPrimary: IdaColors.onPrimary,
    accent: IdaColors.accent,
    accentStrong: IdaColors.accentStrong,
    accentSoft: IdaColors.accentSoft,
    success: IdaColors.success,
    successText: IdaColors.successText,
    successSoft: IdaColors.successSoft,
    danger: IdaColors.danger,
    dangerText: IdaColors.dangerText,
    dangerSoft: IdaColors.dangerSoft,
    warning: IdaColors.warning,
    warningText: IdaColors.warningText,
    warningSoft: IdaColors.warningSoft,
    bg: IdaColors.bg,
    surface: IdaColors.surface,
    surface2: IdaColors.surface2,
    border: IdaColors.border,
    borderStrong: IdaColors.borderStrong,
    dividerStrong: IdaColors.neutral200,
    skeleton: IdaColors.neutral100,
    text: IdaColors.text,
    textSecondary: IdaColors.textSecondary,
    textMuted: IdaColors.textMuted,
    textInverse: IdaColors.textInverse,
    link: IdaColors.link,
    overlay: IdaColors.overlay,
    focusRing: IdaColors.focusRing,
    chart: IdaColors.chartSeriesLight,
    shadowSm: IdaShadows.sm,
    shadowMd: IdaShadows.md,
    shadowLg: IdaShadows.lg,
  );

  static const dark = IdaPalette(
    primary: IdaColors.darkPrimary,
    primaryHover: IdaColors.darkPrimaryHover,
    primaryActive: IdaColors.darkPrimaryActive,
    primaryText: IdaColors.darkPrimaryText,
    primarySoft: IdaColors.darkPrimarySoft,
    onPrimary: IdaColors.onPrimary,
    accent: IdaColors.darkAccent,
    accentStrong: IdaColors.darkAccentStrong,
    accentSoft: IdaColors.darkAccentSoft,
    success: IdaColors.success,
    successText: IdaColors.darkSuccessText,
    successSoft: IdaColors.darkSuccessSoft,
    danger: IdaColors.danger,
    dangerText: IdaColors.darkDangerText,
    dangerSoft: IdaColors.darkDangerSoft,
    warning: IdaColors.warning,
    warningText: IdaColors.darkWarningText,
    warningSoft: IdaColors.darkWarningSoft,
    bg: IdaColors.darkBg,
    surface: IdaColors.darkSurface,
    surface2: IdaColors.darkSurface2,
    border: IdaColors.darkBorder,
    borderStrong: IdaColors.darkBorderStrong,
    dividerStrong: IdaColors.darkBorderStrong,
    skeleton: IdaColors.darkSurface2,
    text: IdaColors.darkText,
    textSecondary: IdaColors.darkTextSecondary,
    textMuted: IdaColors.darkTextMuted,
    textInverse: IdaColors.textInverse,
    link: IdaColors.darkLink,
    overlay: IdaColors.overlay,
    focusRing: IdaColors.focusRing,
    chart: IdaColors.chartSeriesDark,

    shadowSm: <BoxShadow>[],
    shadowMd: <BoxShadow>[],
    shadowLg: IdaShadows.lg,
  );

  @override
  IdaPalette copyWith() => this;

  @override
  IdaPalette lerp(ThemeExtension<IdaPalette>? other, double t) {
    if (other is! IdaPalette) return this;
    Color c(Color a, Color b) => Color.lerp(a, b, t)!;
    return IdaPalette(
      primary: c(primary, other.primary),
      primaryHover: c(primaryHover, other.primaryHover),
      primaryActive: c(primaryActive, other.primaryActive),
      primaryText: c(primaryText, other.primaryText),
      primarySoft: c(primarySoft, other.primarySoft),
      onPrimary: c(onPrimary, other.onPrimary),
      accent: c(accent, other.accent),
      accentStrong: c(accentStrong, other.accentStrong),
      accentSoft: c(accentSoft, other.accentSoft),
      success: c(success, other.success),
      successText: c(successText, other.successText),
      successSoft: c(successSoft, other.successSoft),
      danger: c(danger, other.danger),
      dangerText: c(dangerText, other.dangerText),
      dangerSoft: c(dangerSoft, other.dangerSoft),
      warning: c(warning, other.warning),
      warningText: c(warningText, other.warningText),
      warningSoft: c(warningSoft, other.warningSoft),
      bg: c(bg, other.bg),
      surface: c(surface, other.surface),
      surface2: c(surface2, other.surface2),
      border: c(border, other.border),
      borderStrong: c(borderStrong, other.borderStrong),
      dividerStrong: c(dividerStrong, other.dividerStrong),
      skeleton: c(skeleton, other.skeleton),
      text: c(text, other.text),
      textSecondary: c(textSecondary, other.textSecondary),
      textMuted: c(textMuted, other.textMuted),
      textInverse: c(textInverse, other.textInverse),
      link: c(link, other.link),
      overlay: c(overlay, other.overlay),
      focusRing: c(focusRing, other.focusRing),
      chart: t < 0.5 ? chart : other.chart,
      shadowSm: t < 0.5 ? shadowSm : other.shadowSm,
      shadowMd: t < 0.5 ? shadowMd : other.shadowMd,
      shadowLg: t < 0.5 ? shadowLg : other.shadowLg,
    );
  }
}

extension IdaPaletteContext on BuildContext {
  IdaPalette get ida => Theme.of(this).extension<IdaPalette>() ?? IdaPalette.light;
  TextTheme get text => Theme.of(this).textTheme;
  bool get isDark => Theme.of(this).brightness == Brightness.dark;
}

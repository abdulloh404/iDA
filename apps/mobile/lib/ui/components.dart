import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter/services.dart';

import '../app/app_controller.dart';
import '../data/repository.dart';
import '../l10n/strings.dart';
import '../theme/ida_colors.dart';
import '../theme/ida_palette.dart';
import '../theme/ida_theme.dart';

export '../data/repository.dart' show ApiException;
export '../theme/ida_colors.dart';
export '../theme/ida_palette.dart';
export '../theme/ida_theme.dart';
export '../theme/ida_widgets.dart';


Future<T?> pushPage<T>(BuildContext context, Widget page) =>
    Navigator.of(context).push<T>(MaterialPageRoute(builder: (_) => page));


class ContentWidth extends StatelessWidget {
  const ContentWidth({super.key, required this.child});
  final Widget child;

  @override

  Widget build(BuildContext context) => Center(
        heightFactor: 1,
        child: ConstrainedBox(
          constraints: const BoxConstraints(maxWidth: IdaSizes.maxContentWidth),
          child: child,
        ),
      );
}


class IdaCard extends StatelessWidget {
  const IdaCard({
    super.key,
    required this.child,
    this.padding = const EdgeInsets.all(IdaSpace.s5),
    this.onTap,
    this.color,
    this.semanticLabel,
  });

  final Widget child;
  final EdgeInsetsGeometry padding;
  final VoidCallback? onTap;
  final Color? color;
  final String? semanticLabel;

  @override
  Widget build(BuildContext context) {
    final p = context.ida;
    final body = Padding(padding: padding, child: child);
    return Semantics(
      label: semanticLabel,
      button: onTap != null,
      child: DecoratedBox(
        decoration: BoxDecoration(
          color: color ?? p.surface,
          borderRadius: IdaRadius.xlR,
          boxShadow: p.shadowMd,
          border: context.isDark ? Border.all(color: p.border) : null,
        ),
        child: Material(
          type: MaterialType.transparency,
          borderRadius: IdaRadius.xlR,
          clipBehavior: Clip.antiAlias,
          child: onTap == null ? body : InkWell(onTap: onTap, child: body),
        ),
      ),
    );
  }
}


class SectionHeader extends StatelessWidget {
  const SectionHeader(this.title, {super.key, this.action, this.onAction, this.trailing});

  final String title;
  final String? action;
  final VoidCallback? onAction;
  final Widget? trailing;

  @override
  Widget build(BuildContext context) => Padding(
        padding: const EdgeInsets.only(bottom: IdaSpace.s2),
        child: Row(
          children: [
            Expanded(child: Semantics(header: true, child: Text(title, style: context.text.titleSmall))),
            ?trailing,
            if (action != null)
              TextButton(
                onPressed: onAction,
                style: TextButton.styleFrom(padding: const EdgeInsets.symmetric(horizontal: IdaSpace.s2)),
                child: Row(mainAxisSize: MainAxisSize.min, children: [
                  Text(action!),
                  const SizedBox(width: IdaSpace.s1),
                  const Icon(Icons.chevron_right_rounded, size: IdaSizes.iconMd),
                ]),
              ),
          ],
        ),
      );
}


class KeyValue extends StatelessWidget {
  const KeyValue(this.label, this.value, {super.key, this.valueWidget, this.numeric = false, this.trailing});

  final String label;
  final String? value;
  final Widget? valueWidget;
  final bool numeric;
  final Widget? trailing;

  @override
  Widget build(BuildContext context) {
    final base = context.text.bodyLarge!.copyWith(fontWeight: FontWeight.w500);
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: IdaSpace.s2),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.center,
        children: [
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(label, style: context.text.bodySmall),
                const SizedBox(height: 2),
                valueWidget ??
                    SelectableText(
                      (value == null || value!.isEmpty) ? '-' : value!,
                      style: numeric ? idaNumeric(base) : base,
                    ),
              ],
            ),
          ),
          ?trailing,
        ],
      ),
    );
  }
}


class ExpandableSection extends StatefulWidget {
  const ExpandableSection({
    super.key,
    required this.title,
    required this.icon,
    required this.children,
    this.initiallyExpanded = true,
  });

  final String title;
  final IconData icon;
  final List<Widget> children;
  final bool initiallyExpanded;

  @override
  State<ExpandableSection> createState() => _ExpandableSectionState();
}

class _ExpandableSectionState extends State<ExpandableSection> {
  late bool _open = widget.initiallyExpanded;

  @override
  Widget build(BuildContext context) {
    final p = context.ida;
    return IdaCard(
      padding: EdgeInsets.zero,
      child: Column(
        children: [
          Semantics(
            button: true,
            expanded: _open,
            child: InkWell(
              onTap: () => setState(() => _open = !_open),
              child: Padding(
                padding: const EdgeInsets.fromLTRB(IdaSpace.s5, IdaSpace.s4, IdaSpace.s4, IdaSpace.s4),
                child: Row(children: [
                  IconBox(widget.icon, size: IdaSizes.avatarSm),
                  const SizedBox(width: IdaSpace.s3),
                  Expanded(child: Text(widget.title, style: context.text.titleSmall)),
                  AnimatedRotation(
                    turns: _open ? 0.5 : 0,
                    duration: IdaMotion.of(context, IdaMotion.base),
                    child: Icon(Icons.expand_more_rounded, color: p.textSecondary),
                  ),
                ]),
              ),
            ),
          ),
          AnimatedSize(
            duration: IdaMotion.of(context, IdaMotion.base),
            curve: IdaMotion.ease,
            alignment: Alignment.topCenter,
            child: _open
                ? Padding(
                    padding: const EdgeInsets.fromLTRB(IdaSpace.s5, 0, IdaSpace.s5, IdaSpace.s4),
                    child: Column(children: [
                      Divider(color: p.border),
                      const SizedBox(height: IdaSpace.s1),
                      ...widget.children,
                    ]),
                  )
                : const SizedBox(width: double.infinity),
          ),
        ],
      ),
    );
  }
}


class IconBox extends StatelessWidget {
  const IconBox(this.icon, {super.key, this.size = IdaSizes.tileIcon, this.bg, this.fg, this.iconSize});

  final IconData icon;
  final double size;
  final Color? bg;
  final Color? fg;
  final double? iconSize;

  @override
  Widget build(BuildContext context) {
    final p = context.ida;
    return Container(
      width: size,
      height: size,
      decoration: BoxDecoration(color: bg ?? p.primarySoft, borderRadius: IdaRadius.lgR),
      alignment: Alignment.center,
      child: Icon(icon, size: iconSize ?? size * 0.5, color: fg ?? p.primaryText),
    );
  }
}


class IdaAvatar extends StatelessWidget {
  const IdaAvatar(this.initials, {super.key, this.size = IdaSizes.avatarMd, this.onBrand = false});

  final String initials;
  final double size;


  final bool onBrand;

  @override
  Widget build(BuildContext context) {
    final p = context.ida;
    return Container(
      width: size,
      height: size,
      alignment: Alignment.center,
      decoration: BoxDecoration(
        shape: BoxShape.circle,
        gradient: onBrand ? null : IdaColors.gradientCardAccent,
        color: onBrand ? IdaColors.textInverse.withValues(alpha: 0.18) : null,
        border: Border.all(
          color: onBrand ? IdaColors.textInverse.withValues(alpha: 0.55) : p.surface,
          width: 2,
        ),
      ),
      child: ExcludeSemantics(
        child: Text(
          initials,
          style: TextStyle(
            fontFamily: IdaFonts.display,
            color: IdaColors.textInverse,
            fontWeight: FontWeight.w600,
            fontSize: size * 0.36,
          ),
        ),
      ),
    );
  }
}


class PrimaryButton extends StatelessWidget {
  const PrimaryButton({
    super.key,
    required this.label,
    required this.onPressed,
    this.loading = false,
    this.icon,
    this.expand = true,
    this.destructive = false,
  });

  final String label;
  final VoidCallback? onPressed;
  final bool loading;
  final IconData? icon;
  final bool expand;
  final bool destructive;

  @override
  Widget build(BuildContext context) {
    final p = context.ida;
    final enabled = onPressed != null && !loading;
    final Gradient? gradient = !enabled || destructive || context.isDark ? null : IdaColors.gradientPrimaryButton;
    final bg = !enabled ? p.skeleton : (destructive ? p.danger : p.primary);
    final fg = !enabled && !loading ? p.textMuted : p.onPrimary;
    final child = Row(
      mainAxisSize: MainAxisSize.min,
      mainAxisAlignment: MainAxisAlignment.center,
      children: [
        if (loading)
          SizedBox.square(
            dimension: IdaSizes.iconMd,
            child: CircularProgressIndicator(strokeWidth: 2.2, color: p.accent),
          )
        else if (icon != null)
          Icon(icon, size: IdaSizes.iconMd, color: fg),
        if (loading || icon != null) const SizedBox(width: IdaSpace.s2),
        Flexible(
          child: Text(label,
              overflow: TextOverflow.ellipsis,
              style: context.text.labelLarge!.copyWith(color: fg)),
        ),
      ],
    );
    final button = Semantics(
      button: true,
      enabled: enabled,
      child: AnimatedContainer(
        duration: IdaMotion.of(context, IdaMotion.fast),
        height: IdaSizes.controlHeightLg,
        decoration: BoxDecoration(
          borderRadius: IdaRadius.mdR,
          gradient: gradient,
          color: gradient == null ? (loading ? p.primary : bg) : null,
          boxShadow: enabled && !context.isDark ? p.shadowSm : null,
        ),
        child: Material(
          type: MaterialType.transparency,
          child: InkWell(
            borderRadius: IdaRadius.mdR,
            onTap: enabled ? onPressed : null,
            child: Padding(
              padding: const EdgeInsets.symmetric(horizontal: IdaSpace.s5),
              child: Center(child: child),
            ),
          ),
        ),
      ),
    );
    return expand ? SizedBox(width: double.infinity, child: button) : button;
  }
}


class IdaTextField extends StatefulWidget {
  const IdaTextField({
    super.key,
    required this.label,
    this.controller,
    this.hint,
    this.helper,
    this.error,
    this.obscure = false,
    this.keyboardType,
    this.textInputAction,
    this.onSubmitted,
    this.onChanged,
    this.inputFormatters,
    this.autofillHints,
    this.prefixIcon,
    this.maxLines = 1,
    this.maxLength,
    this.autofocus = false,
    this.enabled = true,
    this.focusNode,
    this.fieldKey,
  });

  final String label;
  final TextEditingController? controller;
  final String? hint;
  final String? helper;
  final String? error;
  final bool obscure;
  final TextInputType? keyboardType;
  final TextInputAction? textInputAction;
  final ValueChanged<String>? onSubmitted;
  final ValueChanged<String>? onChanged;
  final List<TextInputFormatter>? inputFormatters;
  final Iterable<String>? autofillHints;
  final IconData? prefixIcon;
  final int maxLines;
  final int? maxLength;
  final bool autofocus;
  final bool enabled;
  final FocusNode? focusNode;
  final Key? fieldKey;

  @override
  State<IdaTextField> createState() => _IdaTextFieldState();
}

class _IdaTextFieldState extends State<IdaTextField> {
  late bool _hidden = widget.obscure;

  @override
  Widget build(BuildContext context) {
    final p = context.ida;
    final s = S.of(context);
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(widget.label, style: context.text.labelLarge!.copyWith(color: p.textSecondary)),
        const SizedBox(height: IdaSpace.s2),
        TextField(
          key: widget.fieldKey,
          controller: widget.controller,
          focusNode: widget.focusNode,
          obscureText: _hidden,
          enableSuggestions: !widget.obscure,
          autocorrect: !widget.obscure,
          keyboardType: widget.keyboardType,
          textInputAction: widget.textInputAction,
          onSubmitted: widget.onSubmitted,
          onChanged: widget.onChanged,
          inputFormatters: widget.inputFormatters,
          autofillHints: widget.autofillHints,
          maxLines: widget.obscure ? 1 : widget.maxLines,
          maxLength: widget.maxLength,
          autofocus: widget.autofocus,
          enabled: widget.enabled,
          style: context.text.bodyLarge,
          decoration: InputDecoration(
            hintText: widget.hint,
            helperText: widget.error == null ? widget.helper : null,
            helperMaxLines: 3,
            errorText: widget.error,
            fillColor: widget.enabled ? p.surface : p.surface2,
            semanticCounterText: '',
            prefixIcon: widget.prefixIcon == null ? null : Icon(widget.prefixIcon, size: IdaSizes.iconMd),
            suffixIcon: widget.obscure
                ? IconButton(
                    tooltip: _hidden ? s.show : s.hide,
                    icon: Icon(_hidden ? Icons.visibility_outlined : Icons.visibility_off_outlined,
                        size: IdaSizes.iconMd),
                    onPressed: () => setState(() => _hidden = !_hidden),
                  )
                : null,
          ),
        ),
      ],
    );
  }
}


class IdaSegmented<T> extends StatelessWidget {
  const IdaSegmented({super.key, required this.options, required this.value, required this.onChanged, this.onBrand = false});

  final Map<T, String> options;
  final T value;
  final ValueChanged<T> onChanged;


  final bool onBrand;

  @override
  Widget build(BuildContext context) {
    final p = context.ida;
    return Container(
      padding: const EdgeInsets.all(IdaSpace.s1),
      decoration: BoxDecoration(
        color: onBrand ? IdaColors.textInverse.withValues(alpha: 0.16) : p.surface2,
        borderRadius: IdaRadius.fullR,
      ),
      child: Row(
        children: [
          for (final e in options.entries)
            Expanded(
              child: Semantics(
                selected: e.key == value,
                button: true,
                child: GestureDetector(
                  behavior: HitTestBehavior.opaque,
                  onTap: () => onChanged(e.key),
                  child: AnimatedContainer(
                    duration: IdaMotion.of(context, IdaMotion.base),
                    curve: IdaMotion.ease,
                    constraints: const BoxConstraints(minHeight: IdaSizes.chipHeight),
                    alignment: Alignment.center,
                    padding: const EdgeInsets.symmetric(horizontal: IdaSpace.s2, vertical: IdaSpace.s1),
                    decoration: BoxDecoration(
                      color: e.key == value ? (onBrand ? p.surface : p.primary) : Colors.transparent,
                      borderRadius: IdaRadius.fullR,
                      boxShadow: e.key == value && !context.isDark ? p.shadowSm : null,
                    ),
                    child: Text(
                      e.value,
                      maxLines: 1,
                      overflow: TextOverflow.ellipsis,
                      style: context.text.labelLarge!.copyWith(
                        fontWeight: e.key == value ? FontWeight.w700 : FontWeight.w500,
                        color: e.key == value
                            ? (onBrand ? p.primaryText : p.onPrimary)
                            : (onBrand ? IdaColors.textInverse : p.textSecondary),
                      ),
                    ),
                  ),
                ),
              ),
            ),
        ],
      ),
    );
  }
}


class IdaFilterChip extends StatelessWidget {
  const IdaFilterChip({super.key, required this.label, required this.selected, required this.onTap, this.count});

  final String label;
  final bool selected;
  final VoidCallback onTap;
  final int? count;

  @override
  Widget build(BuildContext context) {
    final p = context.ida;
    return Semantics(
      selected: selected,
      button: true,
      child: Material(
        color: selected ? p.accentSoft : p.surface,
        shape: StadiumBorder(side: BorderSide(color: selected ? p.accentStrong : p.border)),
        child: InkWell(
          customBorder: const StadiumBorder(),
          onTap: onTap,
          child: ConstrainedBox(
            constraints: const BoxConstraints(minHeight: IdaSizes.chipHeight),
            child: Padding(
              padding: const EdgeInsets.symmetric(horizontal: IdaSpace.s3),
              child: Row(mainAxisSize: MainAxisSize.min, children: [
                if (selected) ...[
                  Icon(Icons.check_rounded, size: IdaSizes.iconSm, color: p.accentStrong),
                  const SizedBox(width: IdaSpace.s1),
                ],
                Text(label,
                    style: context.text.labelLarge!.copyWith(
                      color: selected ? p.accentStrong : p.textSecondary,
                      fontWeight: selected ? FontWeight.w700 : FontWeight.w500,
                    )),
                if (count != null) ...[
                  const SizedBox(width: IdaSpace.s1),
                  Text('$count', style: idaNumeric(context.text.labelMedium!)),
                ],
              ]),
            ),
          ),
        ),
      ),
    );
  }
}


class EmptyState extends StatelessWidget {
  const EmptyState({super.key, required this.icon, required this.title, this.message, this.action, this.onAction});

  final IconData icon;
  final String title;
  final String? message;
  final String? action;
  final VoidCallback? onAction;

  @override
  Widget build(BuildContext context) => Padding(
        padding: const EdgeInsets.symmetric(horizontal: IdaSpace.s8, vertical: IdaSpace.s10),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Container(
              width: IdaSizes.emptyCircle,
              height: IdaSizes.emptyCircle,
              decoration: const BoxDecoration(shape: BoxShape.circle, gradient: IdaColors.gradientCardAccent),
              child: Icon(icon, size: IdaSizes.iconXl, color: IdaColors.textInverse),
            ),
            const SizedBox(height: IdaSpace.s4),
            Text(title, textAlign: TextAlign.center, style: context.text.titleSmall),
            if (message != null) ...[
              const SizedBox(height: IdaSpace.s1),
              Text(message!, textAlign: TextAlign.center, style: context.text.bodyMedium!.copyWith(color: context.ida.textSecondary)),
            ],
            if (action != null) ...[
              const SizedBox(height: IdaSpace.s4),
              OutlinedButton(onPressed: onAction, child: Text(action!)),
            ],
          ],
        ),
      );
}


class ErrorState extends StatefulWidget {
  const ErrorState({super.key, required this.error, required this.onRetry, this.compact = false});

  final Object error;
  final VoidCallback onRetry;
  final bool compact;

  @override
  State<ErrorState> createState() => _ErrorStateState();
}

class _ErrorStateState extends State<ErrorState> {
  bool _details = false;

  @override
  Widget build(BuildContext context) {
    final s = S.of(context);
    final p = context.ida;
    final e = widget.error;
    final message = e is ApiException ? e.message : s.errorNetwork;
    final trace = e is ApiException ? e.traceId : null;
    return Padding(
      padding: EdgeInsets.symmetric(horizontal: IdaSpace.s6, vertical: widget.compact ? IdaSpace.s4 : IdaSpace.s10),
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          IconBox(Icons.cloud_off_rounded, bg: p.dangerSoft, fg: p.dangerText, size: IdaSizes.emptyCircle),
          const SizedBox(height: IdaSpace.s4),
          Text(s.errorTitle, style: context.text.titleSmall, textAlign: TextAlign.center),
          const SizedBox(height: IdaSpace.s1),
          Text(message, textAlign: TextAlign.center, style: context.text.bodyMedium!.copyWith(color: p.textSecondary)),
          const SizedBox(height: IdaSpace.s4),
          Wrap(
            alignment: WrapAlignment.center,
            spacing: IdaSpace.s2,
            children: [
              OutlinedButton.icon(
                onPressed: widget.onRetry,
                icon: const Icon(Icons.refresh_rounded, size: IdaSizes.iconMd),
                label: Text(s.retry),
              ),
              if (trace != null)
                TextButton(onPressed: () => setState(() => _details = !_details), child: Text(s.details)),
            ],
          ),
          if (_details && trace != null)
            Padding(
              padding: const EdgeInsets.only(top: IdaSpace.s2),
              child: SelectableText('${s.traceId}: $trace', style: idaNumeric(context.text.bodySmall!)),
            ),
        ],
      ),
    );
  }
}


class Skeleton extends StatefulWidget {
  const Skeleton({super.key, this.width, this.height = 14, this.radius = IdaRadius.mdR});

  final double? width;
  final double height;
  final BorderRadius radius;

  @override
  State<Skeleton> createState() => _SkeletonState();
}

class _SkeletonState extends State<Skeleton> with SingleTickerProviderStateMixin {
  late final _c = AnimationController(vsync: this, duration: const Duration(milliseconds: 1100));

  @override
  void didChangeDependencies() {
    super.didChangeDependencies();
    final reduce = MediaQuery.maybeDisableAnimationsOf(context) ?? false;
    if (reduce) {
      _c.stop();
    } else if (!_c.isAnimating) {
      _c.repeat(reverse: true);
    }
  }

  @override
  void dispose() {
    _c.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final c = context.ida.skeleton;
    return FadeTransition(
      opacity: Tween(begin: 0.55, end: 1.0).animate(_c),
      child: Container(
        width: widget.width,
        height: widget.height,
        decoration: BoxDecoration(color: c, borderRadius: widget.radius),
      ),
    );
  }
}


class SkeletonList extends StatelessWidget {
  const SkeletonList({super.key, this.count = 4, this.header = false});
  final int count;
  final bool header;

  @override
  Widget build(BuildContext context) => ListView(
        physics: const NeverScrollableScrollPhysics(),
        padding: IdaSizes.screenPadding,
        children: [
          if (header) ...[
            const Skeleton(height: 140, radius: IdaRadius.xlR),
            const SizedBox(height: IdaSpace.s4),
          ],
          for (var i = 0; i < count; i++) ...[
            IdaCard(
              child: Row(children: [
                const Skeleton(width: 44, height: 44, radius: IdaRadius.lgR),
                const SizedBox(width: IdaSpace.s3),
                Expanded(
                  child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                    Skeleton(width: 120 + 40.0 * (i % 3), height: 14),
                    const SizedBox(height: IdaSpace.s2),
                    const Skeleton(width: 90, height: 12),
                  ]),
                ),
              ]),
            ),
            const SizedBox(height: IdaSpace.s3),
          ],
        ],
      );
}


class AsyncView<T> extends StatefulWidget {
  const AsyncView({
    super.key,
    required this.load,
    required this.builder,
    this.loading,
    this.reloadKey,
  });

  final Future<T> Function() load;
  final Widget Function(BuildContext context, T data, Future<void> Function() reload) builder;
  final Widget? loading;
  final Object? reloadKey;

  @override
  State<AsyncView<T>> createState() => AsyncViewState<T>();
}

class AsyncViewState<T> extends State<AsyncView<T>> {
  T? _data;
  Object? _error;
  bool _loading = true;

  @override
  void initState() {
    super.initState();
    _run();
  }

  @override
  void didUpdateWidget(covariant AsyncView<T> oldWidget) {
    super.didUpdateWidget(oldWidget);
    if (oldWidget.reloadKey != widget.reloadKey) {
      setState(() => _loading = true);
      _run();
    }
  }

  Future<void> reload() => _run();

  Future<void> _run() async {
    try {
      final data = await widget.load();
      if (!mounted) return;
      setState(() {
        _data = data;
        _error = null;
        _loading = false;
      });
    } catch (e) {
      if (!mounted) return;
      setState(() {
        _error = e;
        _loading = false;
      });
    }
  }

  @override
  Widget build(BuildContext context) {
    if (_loading && _data == null) return widget.loading ?? const SkeletonList();
    if (_error != null && _data == null) {
      return ListView(children: [
        ErrorState(
          error: _error!,
          onRetry: () {
            setState(() => _loading = true);
            _run();
          },
        ),
      ]);
    }
    return widget.builder(context, _data as T, reload);
  }
}


String errorText(BuildContext context, Object e) => e is ApiException ? e.message : S.of(context).errorNetwork;

enum ToastTone { success, info, error }


void showToast(BuildContext context, String message, {ToastTone tone = ToastTone.success}) {
  final messenger = AppScope.read(context).messengerKey.currentState ?? ScaffoldMessenger.of(context);
  final icon = switch (tone) {
    ToastTone.success => Icons.check_circle_rounded,
    ToastTone.info => Icons.info_rounded,
    ToastTone.error => Icons.error_rounded,
  };
  final color = switch (tone) {
    ToastTone.success => IdaColors.green400,
    ToastTone.info => IdaColors.cyan300,
    ToastTone.error => IdaColors.darkDangerText,
  };
  messenger
    ..hideCurrentSnackBar()
    ..showSnackBar(SnackBar(
      duration: const Duration(seconds: 4),
      content: Row(children: [
        Icon(icon, color: color, size: IdaSizes.iconMd),
        const SizedBox(width: IdaSpace.s3),
        Expanded(child: Text(message)),
      ]),
    ));
}

void showErrorToast(BuildContext context, Object e) =>
    showToast(context, errorText(context, e), tone: ToastTone.error);


Future<bool> confirmDialog(
  BuildContext context, {
  required String title,
  required String body,
  required String confirmLabel,
  bool destructive = false,
  IconData? icon,
}) async {
  final s = S.of(context);
  final p = context.ida;
  final ok = await showDialog<bool>(
    context: context,
    builder: (ctx) => AlertDialog(
      icon: icon == null
          ? null
          : Center(child: IconBox(icon,
              size: IdaSizes.avatarMd,
              bg: destructive ? p.dangerSoft : p.primarySoft,
              fg: destructive ? p.dangerText : p.primaryText)),
      title: Text(title, textAlign: TextAlign.center),
      content: Text(body, textAlign: TextAlign.center),
      actionsAlignment: MainAxisAlignment.end,
      actions: [
        TextButton(onPressed: () => Navigator.pop(ctx, false), child: Text(s.cancel)),
        FilledButton(
          style: destructive ? FilledButton.styleFrom(backgroundColor: p.danger) : null,
          onPressed: () => Navigator.pop(ctx, true),
          child: Text(confirmLabel),
        ),
      ],
    ),
  );
  return ok ?? false;
}


Future<String?> askReason(BuildContext context, {required String title, String? subtitle}) {
  return showModalBottomSheet<String>(
    context: context,
    isScrollControlled: true,
    builder: (ctx) => _ReasonSheet(title: title, subtitle: subtitle),
  );
}

class _ReasonSheet extends StatefulWidget {
  const _ReasonSheet({required this.title, this.subtitle});
  final String title;
  final String? subtitle;

  @override
  State<_ReasonSheet> createState() => _ReasonSheetState();
}

class _ReasonSheetState extends State<_ReasonSheet> {
  final _c = TextEditingController();
  String? _error;

  @override
  void dispose() {
    _c.dispose();
    super.dispose();
  }

  void _submit() {
    final text = _c.text.trim();
    if (text.isEmpty) {
      setState(() => _error = S.of(context).rejectReasonRequired);
      return;
    }
    Navigator.pop(context, text);
  }

  @override
  Widget build(BuildContext context) {
    final s = S.of(context);
    return Padding(
      padding: EdgeInsets.fromLTRB(
          IdaSpace.s5, 0, IdaSpace.s5, IdaSpace.s5 + MediaQuery.viewInsetsOf(context).bottom),
      child: Column(
        mainAxisSize: MainAxisSize.min,
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Text(widget.title, style: context.text.titleMedium),
          if (widget.subtitle != null) ...[
            const SizedBox(height: IdaSpace.s1),
            Text(widget.subtitle!, style: context.text.bodyMedium!.copyWith(color: context.ida.textSecondary)),
          ],
          const SizedBox(height: IdaSpace.s4),
          IdaTextField(
            fieldKey: const Key('reason'),
            label: s.rejectReason,
            hint: s.rejectReasonHint,
            controller: _c,
            autofocus: true,
            maxLines: 3,
            maxLength: 500,
            error: _error,
            onChanged: (_) {
              if (_error != null) setState(() => _error = null);
            },
          ),
          const SizedBox(height: IdaSpace.s4),
          Row(children: [
            Expanded(child: OutlinedButton(onPressed: () => Navigator.pop(context), child: Text(s.close))),
            const SizedBox(width: IdaSpace.s3),
            Expanded(
              child: FilledButton(
                key: const Key('reason-confirm'),
                style: FilledButton.styleFrom(backgroundColor: context.ida.danger),
                onPressed: _submit,
                child: Text(s.confirm),
              ),
            ),
          ]),
        ],
      ),
    );
  }
}


class BottomActionBar extends StatelessWidget {
  const BottomActionBar({super.key, required this.child});
  final Widget child;

  @override
  Widget build(BuildContext context) {
    final p = context.ida;
    return DecoratedBox(
      decoration: BoxDecoration(
        color: p.surface,
        border: Border(top: BorderSide(color: p.border)),
      ),
      child: SafeArea(
        top: false,
        child: Padding(
          padding: const EdgeInsets.fromLTRB(IdaSpace.s4, IdaSpace.s3, IdaSpace.s4, IdaSpace.s3),
          child: ContentWidth(child: child),
        ),
      ),
    );
  }
}


class Countdown {
  Countdown(this.total, this.onTick) {
    _left = total;
    _timer = Timer.periodic(const Duration(seconds: 1), (t) {
      _left -= const Duration(seconds: 1);
      if (_left <= Duration.zero) {
        _left = Duration.zero;
        t.cancel();
      }
      onTick();
    });
  }

  final Duration total;
  final VoidCallback onTick;
  late Duration _left;
  late final Timer _timer;

  Duration get left => _left;
  bool get done => _left == Duration.zero;
  String get label =>
      '${_left.inMinutes.toString().padLeft(2, '0')}:${(_left.inSeconds % 60).toString().padLeft(2, '0')}';

  void cancel() => _timer.cancel();
}


class IdaSelectField extends StatelessWidget {
  const IdaSelectField({
    super.key,
    required this.label,
    required this.value,
    required this.onTap,
    this.hint,
    this.error,
    this.icon,
  });

  final String label;
  final String? value;
  final String? hint;
  final String? error;
  final IconData? icon;
  final VoidCallback? onTap;

  @override
  Widget build(BuildContext context) {
    final p = context.ida;
    final hasError = error != null;
    return Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
      Text(label, style: context.text.labelLarge!.copyWith(color: p.textSecondary)),
      const SizedBox(height: IdaSpace.s2),
      Semantics(
        button: true,
        label: '$label: ${value ?? hint ?? ''}',
        excludeSemantics: true,
        child: Material(
          color: onTap == null ? p.surface2 : p.surface,
          shape: RoundedRectangleBorder(
            borderRadius: IdaRadius.mdR,
            side: BorderSide(color: hasError ? p.danger : (onTap == null ? p.border : p.borderStrong)),
          ),
          child: InkWell(
            borderRadius: IdaRadius.mdR,
            onTap: onTap,
            child: ConstrainedBox(
              constraints: const BoxConstraints(minHeight: IdaSizes.controlHeightLg + 2),
              child: Padding(
                padding: const EdgeInsets.symmetric(horizontal: IdaSpace.s4),
                child: Row(children: [
                  if (icon != null) ...[
                    Icon(icon, size: IdaSizes.iconMd, color: p.textSecondary),
                    const SizedBox(width: IdaSpace.s3),
                  ],
                  Expanded(
                    child: Text(
                      value ?? hint ?? '',
                      style: context.text.bodyLarge!.copyWith(color: value == null ? p.textMuted : p.text),
                    ),
                  ),
                  Icon(Icons.expand_more_rounded, color: p.textSecondary),
                ]),
              ),
            ),
          ),
        ),
      ),
      if (hasError) ...[
        const SizedBox(height: IdaSpace.s1),
        Row(children: [
          Icon(Icons.error_outline_rounded, size: IdaSizes.iconXs, color: p.dangerText),
          const SizedBox(width: IdaSpace.s1),
          Expanded(child: Text(error!, style: context.text.bodySmall!.copyWith(color: p.dangerText))),
        ]),
      ],
    ]);
  }
}


Future<T?> pickOption<T>(
  BuildContext context, {
  required String title,
  required List<(T, String)> options,
  T? selected,
  bool Function(T value)? enabled,
}) {
  return showModalBottomSheet<T>(
    context: context,
    isScrollControlled: true,
    builder: (ctx) {
      final p = ctx.ida;
      return ConstrainedBox(
        constraints: BoxConstraints(maxHeight: MediaQuery.sizeOf(ctx).height * 0.7),
        child: SafeArea(
          child: Column(mainAxisSize: MainAxisSize.min, crossAxisAlignment: CrossAxisAlignment.stretch, children: [
            Padding(
              padding: const EdgeInsets.fromLTRB(IdaSpace.s5, 0, IdaSpace.s5, IdaSpace.s2),
              child: Text(title, style: ctx.text.titleMedium),
            ),
            Flexible(
              child: ListView(
                shrinkWrap: true,
                padding: const EdgeInsets.only(bottom: IdaSpace.s4),
                children: [
                  for (final (value, label) in options)
                    Builder(builder: (_) {
                      final on = value == selected;
                      final ok = enabled?.call(value) ?? true;
                      return ListTile(
                        enabled: ok,
                        selected: on,
                        selectedTileColor: p.primarySoft,
                        title: Text(label,
                            style: ctx.text.bodyLarge!.copyWith(
                              fontWeight: on ? FontWeight.w700 : FontWeight.w400,
                              color: !ok ? p.textMuted : (on ? p.primaryText : p.text),
                            )),
                        trailing: on ? Icon(Icons.check_rounded, color: p.primaryText) : null,
                        onTap: () => Navigator.pop(ctx, value),
                      );
                    }),
                ],
              ),
            ),
          ]),
        ),
      );
    },
  );
}

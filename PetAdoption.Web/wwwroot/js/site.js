(() => {
    "use strict";

    const init = () => {
        const reduceMotion = window.matchMedia(
            "(prefers-reduced-motion: reduce)"
        );

        const header = document.querySelector("[data-pa-header]");
        const navigation = document.getElementById("paMainNavigation");
        const desktopBreakpoint = window.matchMedia("(min-width: 1200px)");

        if (header) {
            let scrollScheduled = false;

            const updateHeader = () => {
                header.classList.toggle("is-scrolled", window.scrollY > 12);
                scrollScheduled = false;
            };

            updateHeader();

            window.addEventListener(
                "scroll",
                () => {
                    if (scrollScheduled) {
                        return;
                    }

                    scrollScheduled = true;
                    window.requestAnimationFrame(updateHeader);
                },
                { passive: true }
            );
        }

        if (navigation && window.bootstrap?.Collapse) {
            const closeMobileNavigation = () => {
                if (desktopBreakpoint.matches ||
                    !navigation.classList.contains("show")) {
                    return;
                }

                window.bootstrap.Collapse.getOrCreateInstance(navigation, {
                    toggle: false
                }).hide();
            };

            navigation.addEventListener("click", event => {
                if (event.target.closest("a[href]")) {
                    closeMobileNavigation();
                }
            });

            document.addEventListener("click", event => {
                if (header && !header.contains(event.target)) {
                    closeMobileNavigation();
                }
            });

            document.addEventListener("keydown", event => {
                if (event.key === "Escape") {
                    closeMobileNavigation();
                }
            });

            desktopBreakpoint.addEventListener("change", event => {
                if (event.matches && navigation.classList.contains("show")) {
                    window.bootstrap.Collapse.getOrCreateInstance(navigation, {
                        toggle: false
                    }).hide();
                }
            });
        }

        if (!reduceMotion.matches &&
            "IntersectionObserver" in window &&
            Element.prototype.animate) {
            const revealElements = document.querySelectorAll(
                "[data-pa-reveal], " +
                ".pa-page-content > section, " +
                ".pa-page-content > .row, " +
                ".pa-footer-cta"
            );

            const observer = new IntersectionObserver(
                (entries, currentObserver) => {
                    entries.forEach(entry => {
                        if (!entry.isIntersecting) {
                            return;
                        }

                        currentObserver.unobserve(entry.target);

                        const element = entry.target;

                        if (element.hasAttribute("data-pa-no-reveal")) {
                            return;
                        }

                        const animation = element.animate(
                            [
                                {
                                    opacity: 0,
                                    transform: "translate3d(0, 18px, 0)"
                                },
                                {
                                    opacity: 1,
                                    transform: "translate3d(0, 0, 0)"
                                }
                            ],
                            {
                                duration: 650,
                                easing: "cubic-bezier(0.22, 1, 0.36, 1)",
                                fill: "both"
                            }
                        );

                        animation.finished.then(() => {
                            animation.cancel();
                        }).catch(() => { });
                    });
                },
                {
                    threshold: 0.08,
                    rootMargin: "0px 0px -35px 0px"
                }
            );

            revealElements.forEach(element => {
                if (element.hasAttribute("data-pa-no-reveal")) {
                    return;
                }

                if (element.getBoundingClientRect().top >
                    window.innerHeight * 0.7) {
                    observer.observe(element);
                }
            });
        }

        if (!reduceMotion.matches && Element.prototype.animate) {
            document.addEventListener("pointerdown", event => {
                if (!(event.target instanceof Element)) {
                    return;
                }

                const button = event.target.closest(
                    ".pa-btn, .btn-primary, .btn-success"
                );

                if (!button || button.matches(":disabled, .disabled") ||
                    !(button instanceof HTMLElement)) {
                    return;
                }

                const bounds = button.getBoundingClientRect();
                const diameter = Math.max(bounds.width, bounds.height) * 2;
                const ripple = document.createElement("span");

                const left = event.clientX - bounds.left - diameter / 2;
                const top = event.clientY - bounds.top - diameter / 2;

                if (window.getComputedStyle(button).position === "static") {
                    button.style.position = "relative";
                }

                button.style.overflow = "hidden";

                Object.assign(ripple.style, {
                    position: "absolute",
                    left: `${left}px`,
                    top: `${top}px`,
                    width: `${diameter}px`,
                    height: `${diameter}px`,
                    borderRadius: "50%",
                    background: "rgba(255, 255, 255, 0.32)",
                    pointerEvents: "none",
                    zIndex: "0"
                });

                ripple.setAttribute("aria-hidden", "true");
                button.appendChild(ripple);

                const animation = ripple.animate(
                    [
                        { transform: "scale(0)", opacity: 1 },
                        { transform: "scale(1)", opacity: 0 }
                    ],
                    {
                        duration: 570,
                        easing: "ease-out"
                    }
                );

                animation.finished.then(() => ripple.remove())
                    .catch(() => ripple.remove());
            });
        }

        document.querySelectorAll("img.card-img-top").forEach(image => {
            if (!image.hasAttribute("loading")) {
                image.loading = "lazy";
            }

            if (!image.hasAttribute("decoding")) {
                image.decoding = "async";
            }
        });
    };

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", init, { once: true });
    } else {
        init();
    }
})();
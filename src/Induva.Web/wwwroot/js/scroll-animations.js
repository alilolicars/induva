/**
 * INDUVA — GSAP Scroll-Driven Animations
 * GreenSock Animation Platform + ScrollTrigger + ScrollToPlugin
 */

(function () {
    'use strict';

    // Wait until DOM is fully parsed
    document.addEventListener('DOMContentLoaded', initScrollAnimations);

    function initScrollAnimations() {
        if (typeof gsap === 'undefined' || typeof ScrollTrigger === 'undefined') {
            console.warn('GSAP or ScrollTrigger not loaded.');
            return;
        }

        // Register GSAP plugins
        gsap.registerPlugin(ScrollTrigger);
        if (typeof ScrollToPlugin !== 'undefined') {
            gsap.registerPlugin(ScrollToPlugin);
        }

        // 1. SCROLL PROGRESS BAR (at top of viewport)
        const progressBar = document.getElementById('scroll-progress-bar');
        if (progressBar) {
            gsap.to(progressBar, {
                width: '100%',
                ease: 'none',
                scrollTrigger: {
                    trigger: document.body,
                    start: 'top top',
                    end: 'bottom bottom',
                    scrub: 0.25
                }
            });
        }

        // 2. STICKY NAVBAR SCROLL ELEVATION
        const navbar = document.querySelector('header.sticky-top');
        if (navbar) {
            ScrollTrigger.create({
                start: 'top -20',
                onUpdate: (self) => {
                    if (self.progress > 0) {
                        navbar.classList.add('navbar-scrolled');
                    } else {
                        navbar.classList.remove('navbar-scrolled');
                    }
                }
            });
        }

        // 3. SMOOTH ANCHOR SCROLLING
        document.querySelectorAll('a[href^="#"]:not([href="#"]):not([data-bs-toggle])').forEach(anchor => {
            anchor.addEventListener('click', function (e) {
                const targetId = this.getAttribute('href');
                if (targetId && targetId.length > 1) {
                    const targetEl = document.querySelector(targetId);
                    if (targetEl) {
                        e.preventDefault();
                        if (typeof ScrollToPlugin !== 'undefined') {
                            gsap.to(window, {
                                duration: 0.8,
                                scrollTo: { y: targetEl, offsetY: 72 },
                                ease: 'power2.inOut'
                            });
                        } else {
                            targetEl.scrollIntoView({ behavior: 'smooth' });
                        }
                    }
                }
            });
        });

        // 4. LANDING PAGE SPECIFIC SCROLL ANIMATIONS
        initLandingPageAnimations();

        // 5. GENERIC REVEAL ON SCROLL (.gsap-reveal)
        document.querySelectorAll('.gsap-reveal').forEach(el => {
            gsap.from(el, {
                scrollTrigger: {
                    trigger: el,
                    start: 'top 85%',
                    toggleActions: 'play none none none'
                },
                y: 30,
                opacity: 0,
                duration: 0.7,
                ease: 'power2.out'
            });
        });
    }

    function initLandingPageAnimations() {
        // Hero Section Elements
        const heroHeadline = document.querySelector('.display-headline');
        if (heroHeadline) {
            gsap.from(heroHeadline, {
                opacity: 0,
                y: 28,
                duration: 0.85,
                ease: 'power3.out'
            });
        }

        const heroImg = document.querySelector('img[alt="Egyptian Factory Processing"]');
        if (heroImg) {
            gsap.from(heroImg, {
                opacity: 0,
                scale: 0.94,
                y: 20,
                duration: 1,
                ease: 'power2.out',
                delay: 0.15
            });

            // Gentle parallax scrub on hero image while scrolling
            gsap.to(heroImg, {
                scrollTrigger: {
                    trigger: heroImg,
                    start: 'top center',
                    end: 'bottom top',
                    scrub: 1.2
                },
                y: -24,
                ease: 'none'
            });
        }

        // Facts Strip Card Reveal & Animated Count-Up
        const factsCard = document.getElementById('facts-strip-card');
        if (factsCard) {
            gsap.from(factsCard, {
                scrollTrigger: {
                    trigger: factsCard,
                    start: 'top 85%',
                    toggleActions: 'play none none none'
                },
                opacity: 0,
                y: 35,
                duration: 0.8,
                ease: 'power2.out'
            });

            const counters = factsCard.querySelectorAll('.count-up');
            counters.forEach(counter => {
                const target = parseInt(counter.getAttribute('data-target'), 10) || 0;
                const tracker = { val: 0 };

                gsap.to(tracker, {
                    scrollTrigger: {
                        trigger: factsCard,
                        start: 'top 85%',
                        toggleActions: 'play none none none'
                    },
                    val: target,
                    duration: 1.6,
                    ease: 'power2.out',
                    onUpdate: function () {
                        counter.textContent = Math.round(tracker.val);
                    }
                });
            });
        }

        // How It Works Step Cards
        const stepCards = document.querySelectorAll('#how-it-works .card-induva, section:has(.font-mono:contains("01")) .col-md-4');
        const howItWorksSection = document.querySelector('section:nth-of-type(3)');
        if (howItWorksSection) {
            const cards = howItWorksSection.querySelectorAll('.col-md-4');
            if (cards.length > 0) {
                gsap.from(cards, {
                    scrollTrigger: {
                        trigger: howItWorksSection,
                        start: 'top 78%',
                        toggleActions: 'play none none none'
                    },
                    y: 40,
                    opacity: 0,
                    duration: 0.75,
                    stagger: 0.16,
                    ease: 'power2.out'
                });
            }
        }

        // Features Grid Cards
        const featuresSection = document.querySelector('section.bg-white.border-top.border-bottom');
        if (featuresSection) {
            const featureCols = featuresSection.querySelectorAll('.col-md-6');
            if (featureCols.length > 0) {
                gsap.from(featureCols, {
                    scrollTrigger: {
                        trigger: featuresSection,
                        start: 'top 75%',
                        toggleActions: 'play none none none'
                    },
                    y: 35,
                    opacity: 0,
                    duration: 0.65,
                    stagger: 0.1,
                    ease: 'power2.out'
                });
            }
        }

        // Who It's For (Audience / Links in the Chain)
        const audienceSection = document.querySelector('section:has(.card-induva:has(img[alt="Factory owners"]))');
        if (audienceSection) {
            const cols = audienceSection.querySelectorAll('.col');
            if (cols.length > 0) {
                gsap.from(cols, {
                    scrollTrigger: {
                        trigger: audienceSection,
                        start: 'top 80%',
                        toggleActions: 'play none none none'
                    },
                    y: 25,
                    opacity: 0,
                    duration: 0.6,
                    stagger: 0.08,
                    ease: 'power2.out'
                });
            }
        }

        // Example Band (Tomato Canning Showcase)
        const exampleBand = document.querySelector('section[style*="background-color: #0E1B24"]');
        if (exampleBand) {
            const exampleText = exampleBand.querySelector('.col-lg-6:first-child');
            const exampleImg = exampleBand.querySelector('img[alt*="Tomato canning"]');

            if (exampleText) {
                gsap.from(exampleText, {
                    scrollTrigger: {
                        trigger: exampleBand,
                        start: 'top 78%',
                        toggleActions: 'play none none none'
                    },
                    x: -30,
                    opacity: 0,
                    duration: 0.8,
                    ease: 'power2.out'
                });
            }

            if (exampleImg) {
                gsap.from(exampleImg, {
                    scrollTrigger: {
                        trigger: exampleBand,
                        start: 'top 78%',
                        toggleActions: 'play none none none'
                    },
                    x: 30,
                    opacity: 0,
                    duration: 0.8,
                    ease: 'power2.out',
                    delay: 0.1
                });
            }
        }

        // Final CTA Section
        const finalCta = document.querySelector('section:last-of-type .card-induva');
        if (finalCta) {
            gsap.from(finalCta, {
                scrollTrigger: {
                    trigger: finalCta,
                    start: 'top 85%',
                    toggleActions: 'play none none none'
                },
                y: 30,
                opacity: 0,
                duration: 0.75,
                ease: 'power2.out'
            });
        }
    }
})();


Feature: Immersive praying views
  As a visitor praying a single prayer, a devotion, or an hour
  I want a pinned bottom bar with the controls I need
  So that I can pray without the rest of the site getting in the way

  # ---------- Single prayers ----------

  Scenario: A prayer shows the bottom bar with a way back to all prayers
	Given I open the "discover/prayers/hail-mary" prayer directly
	Then the prayer reader bar should be visible
	And the prayer reader bar should link back to all prayers
	And the page should be in immersive mode

  Scenario: A prayer with a Latin text can be switched to Latin and back
	Given I open the "discover/prayers/hail-mary" prayer directly
	When I switch the prayer language
	Then the prayer text should be in Latin
	When I switch the prayer language
	Then the prayer text should be in English

  Scenario: On a phone the reader bar opens the quick menu
	Given I am on a phone-sized screen
	And I open the "discover/prayers/hail-mary" prayer directly
	When I open the quick menu from the reader bar
	Then the quick menu should be open

  Scenario: Leaving a prayer restores the site chrome
	Given I open the "discover/prayers/hail-mary" prayer directly
	When I leave the prayer from the reader bar
	Then I should be on the prayers browser
	And the page should not be in immersive mode

  Scenario: The quick menu's theme picker switches the theme
	Given I am on a phone-sized screen
	And I open the "discover/prayers/hail-mary" prayer directly
	When I open the quick menu from the reader bar
	And I choose the dark theme in the quick menu
	Then the page should use the dark theme

  Scenario: On a phone the site chrome steps aside for a prayer
	Given I am on a phone-sized screen
	And I open the "discover/prayers/hail-mary" prayer directly
	Then the masthead should be hidden
	And the floating guide orb should be hidden
	And the back-to-top arrow should be hidden

  Scenario: On a phone the Guide to Prayers opens from the reader bar
	Given I am on a phone-sized screen
	And I open the "discover/prayers/hail-mary" prayer directly
	When I open the prayer guide from the reader bar
	Then the prayer guide dialog should be visible

  # ---------- Devotions ----------

  Scenario: Starting a devotion opens at the top of the thread
	Given I open the devotions browser
	When I choose the first devotion
	And I begin praying
	Then the page should be scrolled to the top
	And the page should be in immersive mode

  Scenario: Next in a devotion keeps the current step clear of the bottom bar
	Given I am on a phone-sized screen
	And I open the devotions browser
	When I choose the first devotion
	And I begin praying
	And I press Next 8 times in the player bar
	Then the current devotion step should sit above the player bar
	And the current devotion step should be below the top of the screen

  Scenario: Back in a devotion steps to the previous prayer
	Given I open the devotions browser
	When I choose the first devotion
	And I begin praying
	And I press Next 2 times in the player bar
	And I press Back in the player bar
	Then the player bar should show 1 completed step

  Scenario: The back-to-top arrow stays hidden while praying a devotion
	Given I am on a phone-sized screen
	And I open the devotions browser
	When I choose the first devotion
	And I begin praying
	And I press Next 8 times in the player bar
	Then the back-to-top arrow should be hidden

  # ---------- Liturgy of the Hours ----------

  Scenario: An hour opens in immersive mode with the player bar
	Given I open the "morning" hour
	Then the player bar should be visible
	And the page should be in immersive mode

  Scenario: Next in an hour keeps the current step clear of the bottom bar
	Given I am on a phone-sized screen
	And I open the "morning" hour
	When I press Next 3 times in the player bar
	Then the current hour step should start above the player bar

  Scenario: Advancing into a new part of an hour keeps its heading in view
	Given I open the "morning" hour
	When I advance until the current hour step starts a new part
	Then that part's heading should be in the viewport
